/*
 *	Firebird ADO.NET Data provider for .NET and Mono 
 * 
 *	   The contents of this file are subject to the Initial 
 *	   Developer's Public License Version 1.0 (the "License"); 
 *	   you may not use this file except in compliance with the 
 *	   License. You may obtain a copy of the License at 
 *	   http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *	   Software distributed under the License is distributed on 
 *	   an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either 
 *	   express or implied. See the License for the specific 
 *	   language governing rights and limitations under the License.
 * 
 *	Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *	All Rights Reserved.
 * 
 *  Contributors:
 *      Jiri Cincura (jiri@cincura.net)
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Diagnostics;
using System.IO;

using FirebirdSql.Data.Common;
using FirebirdSql.Data.Schema;


namespace FirebirdSql.Data.FirebirdClient
{
	internal class FbConnectionInternal : MarshalByRefObject, IDisposable
	{
		#region · Fields ·

		private IDatabase db;
		private FbTransaction activeTransaction;
		private List<WeakReference> preparedCommands;
		private FbConnectionString _options;
		private FbConnection _owningConnection;
		private bool disposed;
		private object preparedCommandsCleanupSyncRoot;
		private FbInternalResourceManager _resourceManager;
		//private FbPromotableSinglePhaseNotification _promotableFbTransactionEnlister;

		#endregion

		#region · Properties ·

		public IDatabase Database
		{
			get { return db; }
		}

		public bool HasActiveTransaction
		{
			get
			{
				return activeTransaction != null && !activeTransaction.IsUpdated;
			}
		}

		public FbTransaction ActiveTransaction
		{
			get { return activeTransaction; }
		}

		public FbConnection OwningConnection
		{
			get { return _owningConnection; }
		}

		public bool IsEnlisted
		{
			get { return _resourceManager != null && !_resourceManager.IsCompleted; }
		}

		public FbConnectionString Options
		{
			get { return _options; }
		}

		public bool CancelDisabled { get; set; }

		#endregion

		#region · Constructors ·

		public FbConnectionInternal(FbConnectionString options)
		{
			preparedCommands = new List<WeakReference>();
			preparedCommandsCleanupSyncRoot = new object();

			_options = options;

			GC.SuppressFinalize(this);
		}

		#endregion

		#region · Finalizer ·

		~FbConnectionInternal()
		{
			// Do not re-create Dispose clean-up code here.
			// Calling Dispose(false) is optimal in terms of
			// readability and maintainability.
			Dispose(false);
		}

		#endregion

		#region · IDisposable Methods ·

		public void Dispose()
		{
			Dispose(true);

			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue 
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			lock (this)
			{
				if (!disposed)
				{
					// release any unmanaged resources
					Disconnect();

					if (disposing)
					{
						// release managed resources here
					}

					disposed = true;
				}
			}
		}

		#endregion

		#region · Create and Drop database methods ·

		public void CreateDatabase(DatabaseParameterBuffer dpb)
		{
			IDatabase db = ClientFactory.CreateDatabase(_options);
			db.CreateDatabase(dpb, _options.DataSource, _options.Port, _options.Database);
		}

		public void DropDatabase()
		{
			IDatabase db = ClientFactory.CreateDatabase(_options);
			db.Attach(BuildDpb(db, _options), _options.DataSource, _options.Port, _options.Database);
			db.DropDatabase();
		}

		#endregion

		#region · Connect and Disconnect methods ·

		public void Connect()
		{
			if (Charset.GetCharset(_options.Charset) == null)
			{
				throw new FbException("Invalid character set specified");
			}

			try
			{
				db = ClientFactory.CreateDatabase(_options);
				db.Charset = Charset.GetCharset(_options.Charset);
				db.Dialect = _options.Dialect;
				db.PacketSize = _options.PacketSize;

				DatabaseParameterBuffer dpb = BuildDpb(db, _options);

				if (_options.FallIntoTrustedAuth)
				{
					db.AttachWithTrustedAuth(dpb, _options.DataSource, _options.Port, _options.Database);
				}
				else
				{
					db.Attach(dpb, _options.DataSource, _options.Port, _options.Database);
				}
			}
			catch (IscException ex)
			{
				throw new FbException(ex.Message, ex);
			}
		}

		public void Disconnect()
		{
			if (db != null)
			{
				try
				{
					db.Dispose();
				}
				catch
				{
				}
				finally
				{
					db = null;
					_owningConnection = null;
					_options = null;
				}
			}
		}

		#endregion

		#region · Transaction Handling Methods ·

		public FbTransaction BeginTransaction(IsolationLevel level, string transactionName)
		{
			lock (this)
			{
				if (HasActiveTransaction)
				{
					throw new InvalidOperationException("A transaction is currently active. Parallel transactions are not supported.");
				}

				try
				{
					activeTransaction = new FbTransaction(_owningConnection, level);
					activeTransaction.BeginTransaction(this);

					if (transactionName != null)
					{
						activeTransaction.Save(transactionName);
					}
				}
				catch (IscException ex)
				{
					throw new FbException(ex.Message, ex);
				}
			}

			return activeTransaction;
		}

		public FbTransaction BeginTransaction(FbTransactionOptions options, string transactionName)
		{
			lock (this)
			{
				if (HasActiveTransaction)
				{
					throw new InvalidOperationException("A transaction is currently active. Parallel transactions are not supported.");
				}

				try
				{
					activeTransaction = new FbTransaction(_owningConnection, IsolationLevel.Unspecified);

					activeTransaction.BeginTransaction(options);

					if (transactionName != null)
					{
						activeTransaction.Save(transactionName);
					}
				}
				catch (IscException ex)
				{
					throw new FbException(ex.Message, ex);
				}
			}

			return activeTransaction;
		}

		public void DisposeTransaction()
		{
			if (activeTransaction != null && !IsEnlisted)
			{
				activeTransaction.Dispose();
				activeTransaction = null;
			}
		}

		public void TransactionUpdated()
		{
			for (int i = 0; i < preparedCommands.Count; i++)
			{
				if (!preparedCommands[i].IsAlive)
					continue;

				FbCommand command = preparedCommands[i].Target as FbCommand;

				if (command.Transaction != null)
				{
					command.CloseReader();
					command.Transaction = null;
				}
			}
		}

		#endregion

		#region · Transaction Enlistement ·

		public void EnlistTransaction(System.Transactions.Transaction transaction)
		{
			if (_resourceManager != null && _resourceManager.SystemTransaction == transaction)
				return;

			if (HasActiveTransaction)
				throw new ArgumentException("Unable to enlist in transaction, a local transaction already exists");

			if (_resourceManager != null)
				throw new ArgumentException("Already enlisted in a transaction");

			_resourceManager = new FbInternalResourceManager(this);
			_resourceManager.Enlist(transaction);
		}

		internal void PromotableLocalTransactionCompleted()
		{
			_resourceManager = null;
			DisposeTransaction();
		}

		public FbTransaction BeginTransaction(System.Transactions.IsolationLevel isolationLevel)
		{
			switch (isolationLevel)
			{
				case System.Transactions.IsolationLevel.Chaos:
					return BeginTransaction(System.Data.IsolationLevel.Chaos, null);

				case System.Transactions.IsolationLevel.ReadUncommitted:
					return BeginTransaction(System.Data.IsolationLevel.ReadUncommitted, null);

				case System.Transactions.IsolationLevel.RepeatableRead:
					return BeginTransaction(System.Data.IsolationLevel.RepeatableRead, null);

				case System.Transactions.IsolationLevel.Serializable:
					return BeginTransaction(System.Data.IsolationLevel.Serializable, null);

				case System.Transactions.IsolationLevel.Snapshot:
					return BeginTransaction(System.Data.IsolationLevel.Snapshot, null);

				case System.Transactions.IsolationLevel.Unspecified:
					return BeginTransaction(System.Data.IsolationLevel.Unspecified, null);

				case System.Transactions.IsolationLevel.ReadCommitted:
				default:
					return BeginTransaction(System.Data.IsolationLevel.ReadCommitted, null);
			}
		}

		#endregion

		#region · Schema Methods ·

		public DataTable GetSchema(string collectionName, string[] restrictions)
		{
			return FbSchemaFactory.GetSchema(_owningConnection, collectionName, restrictions);
		}

		#endregion

		#region · Prepared Commands Methods ·

		public void AddPreparedCommand(FbCommand command)
		{
			int position = preparedCommands.Count;
			for (int i = 0; i < preparedCommands.Count; i++)
			{
				if (!preparedCommands[i].IsAlive)
				{
					position = i;
					break;
				}
				if (preparedCommands[i].Target == command)
				{
					return;
				}
			}
			preparedCommands.Insert(position, new WeakReference(command));
		}

		public void RemovePreparedCommand(FbCommand command)
		{
			lock (preparedCommandsCleanupSyncRoot)
			{
				for (int i = preparedCommands.Count - 1; i >= 0; i--)
				{
					var cmd = preparedCommands[i];
					if (cmd != null && cmd.Target == command)
					{
						cmd.Target = null;
						preparedCommands.RemoveAt(i);
						return;
					}
				}
			}
		}

		public void ReleasePreparedCommands()
		{
			WeakReference[] toProcess = new WeakReference[preparedCommands.Count];
			preparedCommands.CopyTo(toProcess);
			for (int i = 0; i < toProcess.Length; i++)
			{
				WeakReference current = toProcess[i];

				if (!current.IsAlive)
					continue;

				try
				{
					// Release statement handle
					(current.Target as FbCommand).Release();
				}
				catch (System.IO.IOException)
				{
					// If an IO error occurs weh trying to release the command 
					// avoid it. ( It maybe the connection to the server was down 
					// for unknown reasons. )
				}
				catch (IscException ex)
				{
					if (ex.ErrorCode != IscCodes.isc_net_read_err &&
						ex.ErrorCode != IscCodes.isc_net_write_err &&
						ex.ErrorCode != IscCodes.isc_network_error)
					{
						throw;
					}
				}
			}

			lock (preparedCommandsCleanupSyncRoot)
			{
				preparedCommands.Clear();
			}
		}

		#endregion

		#region · Firebird Events Methods ·

		public void CloseEventManager()
		{
			if (db != null && db.HasRemoteEventSupport)
			{
				lock (db)
				{
					db.CloseEventManager();
				}
			}
		}

		#endregion

		#region · Connection Verification ·

		public bool Verify()
		{
			// Do not actually ask for any information
			byte[] items = new byte[]
			{
				IscCodes.isc_info_end
			};

			try
			{
				db.GetDatabaseInfo(items, 16);

				return true;
			}
			catch
			{
				return false;
			}
		}

		#endregion

		#region · Private Methods ·

		private DatabaseParameterBuffer BuildDpb(IDatabase db, FbConnectionString options)
		{
			DatabaseParameterBuffer dpb = new DatabaseParameterBuffer();

			dpb.Append(IscCodes.isc_dpb_version1);
			dpb.Append(IscCodes.isc_dpb_dummy_packet_interval, new byte[] { 120, 10, 0, 0 });
			dpb.Append(IscCodes.isc_dpb_sql_dialect, new byte[] { Convert.ToByte(options.Dialect), 0, 0, 0 });
			dpb.Append(IscCodes.isc_dpb_lc_ctype, options.Charset);
			if (options.DbCachePages > 0)
			{
				dpb.Append(IscCodes.isc_dpb_num_buffers, options.DbCachePages);
			}
			if (!string.IsNullOrEmpty(options.Role))
			{
				dpb.Append(IscCodes.isc_dpb_sql_role_name, options.Role);
			}
			dpb.Append(IscCodes.isc_dpb_connect_timeout, options.ConnectionTimeout);

			if (!options.FallIntoTrustedAuth)
			{
				dpb.Append(IscCodes.isc_dpb_user_name, options.UserID);
				dpb.Append(IscCodes.isc_dpb_password, options.Password);
			}
			dpb.Append(IscCodes.isc_dpb_process_id, GetProcessID());
			dpb.Append(IscCodes.isc_dpb_process_name, GetProcessName());
			if (options.NoDatabaseTriggers)
			{
				dpb.Append(IscCodes.isc_dpb_no_db_triggers, 1);
			}

			return dpb;
		}

		private string GetProcessName()
		{
			// showing ApplicationPhysicalPath may be wrong because of connection pooling; better idea?
			return GetHostingPath() ?? GetRealProcessName();
		}


		private string GetHostingPath()
		{
			System.Reflection.Assembly assembly;
			try
			{
				assembly = System.Reflection.Assembly.Load(string.Format("System.Web, Version={0}.{1}.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", Environment.Version.Major, Environment.Version.Minor));
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			catch (FileLoadException)
			{
				return null;
			}
			catch (BadImageFormatException)
			{
				return null;
			}
			return (string)assembly
				.GetType("System.Web.Hosting.HostingEnvironment")
				.GetProperty("ApplicationPhysicalPath")
				.GetValue(null, null);
		}
		private string GetRealProcessName()
		{
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
			if (assembly != null)
			{
				return assembly.Location;
			}
			else // if we're not loaded from managed code
			{
				return Process.GetCurrentProcess().MainModule.FileName;
			}
		}

		private int GetProcessID()
		{
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
			if (assembly != null)
			{
#if (!NET_35)
				if (assembly.IsFullyTrusted)
					return Process.GetCurrentProcess().Id;
				else
					return -1;
#else
				// Think about better solution
				try
				{
					return Process.GetCurrentProcess().Id;
				}
				catch
				{
					return -1;
				}
#endif
			}
			else // if we're not loaded from managed code
			{
				return Process.GetCurrentProcess().Id;
			}
		}
		#endregion

		#region Cancelation
		public void EnableCancel()
		{
			db.CancelOperation(IscCodes.fb_cancel_enable);
			CancelDisabled = false;
		}

		public void DisableCancel()
		{
			db.CancelOperation(IscCodes.fb_cancel_disable);
			CancelDisabled = true;
		}

		public void CancelCommand()
		{
			db.CancelOperation(IscCodes.fb_cancel_raise);
		}
		#endregion

		#region Infrastructure
		public FbConnectionInternal SetOwningConnection(FbConnection owningConnection)
		{
			_owningConnection = owningConnection;
			return this;
		}
		#endregion
	}
}
