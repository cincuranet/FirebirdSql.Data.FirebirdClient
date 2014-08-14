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
 */

using System;
using System.Data;
using System.IO;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed.Version10
{
	internal sealed class GdsTransaction : ITransaction, IDisposable
	{
		#region · Events ·

		public event TransactionUpdateEventHandler Update;

		#endregion

		#region · Fields ·

		private int					handle;
		private bool				disposed;
		private GdsDatabase			database;
		private TransactionState	_state;
		private object				stateSyncRoot;

		#endregion

		#region · Properties ·

		public int Handle
		{
			get { return handle; }
		}

		public TransactionState State
		{
			get { return _state; }
		}

		#endregion

		#region · Constructors ·

		private GdsTransaction()
		{
			stateSyncRoot = new object();
		}

		public GdsTransaction(IDatabase db)
			: this()
		{
			if (!(db is GdsDatabase))
			{
				throw new ArgumentException("Specified argument is not of GdsDatabase type.");
			}

			database = (GdsDatabase)db;
			_state = TransactionState.NoTransaction;

			GC.SuppressFinalize(this);
		}

		#endregion

		#region · Finalizer ·

		~GdsTransaction()
		{
			Dispose(false);
		}

		#endregion

		#region · IDisposable methods ·

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			lock (stateSyncRoot)
			{
				if (!disposed)
				{
					try
					{
						// release any unmanaged resources
						Rollback();
					}
					catch
					{
					}
					finally
					{
						// release any managed resources
						if (disposing)
						{
							database     = null;
							handle = 0;
							_state  = TransactionState.NoTransaction;
						}
						
						disposed = true;
					}
				}
			}
		}

		#endregion

		#region · Methods ·

		public void BeginTransaction(TransactionParameterBuffer tpb)
		{
			lock (stateSyncRoot)
			{
				if (_state != TransactionState.NoTransaction)
				{
					throw GetNoValidTransactionException();
				}

				try
				{
					GenericResponse response;
					lock (database.SyncObject)
					{
						database.Write(IscCodes.op_transaction);
						database.Write(database.Handle);
						database.WriteBuffer(tpb.ToArray());
						database.Flush();

						response = database.ReadGenericResponse();

						database.TransactionCount++;
					}

					handle = response.ObjectHandle;
					_state = TransactionState.Active;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		public void Commit()
		{
			lock (stateSyncRoot)
			{
				CheckTransactionState();

				try
				{
					lock (database.SyncObject)
					{
						database.Write(IscCodes.op_commit);
						database.Write(handle);
						database.Flush();

						database.ReadResponse();

						database.TransactionCount--;
					}

					if (Update != null)
					{
						Update(this, new EventArgs());
					}

					_state = TransactionState.NoTransaction;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		public void Rollback()
		{
			lock (stateSyncRoot)
			{
				CheckTransactionState();

				try
				{
					lock (database.SyncObject)
					{
						database.Write(IscCodes.op_rollback);
						database.Write(handle);
						database.Flush();

						database.ReadResponse();

						database.TransactionCount--;
					}

					if (Update != null)
					{
						Update(this, new EventArgs());
					}

					_state = TransactionState.NoTransaction;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		public void CommitRetaining()
		{
			lock (stateSyncRoot)
			{
				CheckTransactionState();

				try
				{
					lock (database.SyncObject)
					{
						database.Write(IscCodes.op_commit_retaining);
						database.Write(handle);
						database.Flush();

						database.ReadResponse();
					}

					_state = TransactionState.Active;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		public void RollbackRetaining()
		{
			lock (stateSyncRoot)
			{
				CheckTransactionState();

				try
				{
					lock (database.SyncObject)
					{
						database.Write(IscCodes.op_rollback_retaining);
						database.Write(handle);
						database.Flush();

						database.ReadResponse();
					}

					_state = TransactionState.Active;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		#endregion

		#region · Two Phase Commit Methods ·

		public void Prepare()
		{
			lock (stateSyncRoot)
			{
				CheckTransactionState();

				try
				{
					_state = TransactionState.NoTransaction;

					lock (database.SyncObject)
					{
						database.Write(IscCodes.op_prepare);
						database.Write(handle);
						database.Flush();

						database.ReadResponse();
					}

					_state = TransactionState.Prepared;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		public void Prepare(byte[] buffer)
		{
			lock (stateSyncRoot)
			{
				CheckTransactionState();

				try
				{
					_state = TransactionState.NoTransaction;

					lock (database.SyncObject)
					{
						database.Write(IscCodes.op_prepare2);
						database.Write(handle);
						database.WriteBuffer(buffer, buffer.Length);
						database.Flush();

						database.ReadResponse();
					}

					_state = TransactionState.Prepared;
				}
				catch (IOException)
				{
					throw new IscException(IscCodes.isc_net_read_err);
				}
			}
		}

		#endregion

		#region · Private Methods ·

		private void CheckTransactionState()
		{
			if (_state == TransactionState.NoTransaction)
			{
				throw GetNoValidTransactionException();
			}
		}

		private IscException GetNoValidTransactionException()
		{
			return new IscException(IscCodes.isc_arg_gds, IscCodes.isc_tra_state, handle, "no valid");
		}

		#endregion
	}
}
