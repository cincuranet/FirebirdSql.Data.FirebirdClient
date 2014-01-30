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
 *    Jiri Cincura (jiri@cincura.net)
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.FirebirdClient
{
	public sealed class FbCommand : DbCommand, ICloneable
	{
		#region � Fields �

		private CommandType commandType;
		private UpdateRowSource updatedRowSource;
		private FbConnection connection;
		private FbTransaction transaction;
		private FbParameterCollection parameters;
		private StatementBase statement;
		private FbDataReader activeReader;
		private List<string> namedParameters;
		private string commandText;
		private bool disposed;
		private bool designTimeVisible;
		private bool implicitTransaction;
		private int commandTimeout;
		private int fetchSize;

		#endregion

		#region � Properties �

		[Category("Data")]
		[DefaultValue("")]
		[RefreshProperties(RefreshProperties.All)]
		public override string CommandText
		{
			get { return this.commandText; }
			set
			{
				lock (this)
				{
					if (this.commandText != value && this.statement != null)
					{
						this.Release();
					}

					this.commandText = value;
				}
			}
		}

		[Category("Data")]
		[DefaultValue(CommandType.Text), RefreshProperties(RefreshProperties.All)]
		public override CommandType CommandType
		{
			get { return this.commandType; }
			set { this.commandType = value; }
		}

		public override int CommandTimeout
		{
			get { return this.commandTimeout; }
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("The property value assigned is less than 0.");
				}

				this.commandTimeout = value;
			}
		}

		[Browsable(false)]
		public string CommandPlan
		{
			get
			{
				if (this.statement != null)
				{
					return this.statement.GetExecutionPlan();
				}
				return null;
			}
		}

		[Category("Behavior")]
		[DefaultValue(null)]
		public new FbConnection Connection
		{
			get { return this.connection; }
			set
			{
				lock (this)
				{
					if (this.activeReader != null)
					{
						throw new InvalidOperationException("There is already an open DataReader associated with this Command which must be closed first.");
					}

					if (this.transaction != null && this.transaction.IsUpdated)
					{
						this.transaction = null;
					}

					if (this.connection != null &&
						this.connection != value &&
						this.connection.State == ConnectionState.Open)
					{
						this.Release();
					}

					this.connection = value;
				}
			}
		}

		[Category("Data")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public new FbParameterCollection Parameters
		{
			get
			{
				if (this.parameters == null)
				{
					this.parameters = new FbParameterCollection();
				}
				return this.parameters;
			}
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new FbTransaction Transaction
		{
			get { return this.implicitTransaction ? null : this.transaction; }
			set
			{
				lock (this)
				{
					if (this.activeReader != null)
					{
						throw new InvalidOperationException("There is already an open DataReader associated with this Command which must be closed first.");
					}

					this.RollbackImplicitTransaction();

					this.transaction = value;

					if (this.statement != null)
					{
						if (this.transaction != null)
						{
							this.statement.Transaction = this.transaction.Transaction;
						}
						else
						{
							this.statement.Transaction = null;
						}
					}
				}
			}
		}

		[Category("Behavior")]
		[DefaultValue(UpdateRowSource.Both)]
		public override UpdateRowSource UpdatedRowSource
		{
			get { return this.updatedRowSource; }
			set { this.updatedRowSource = value; }
		}

		[Category("Behavior")]
		[DefaultValue(200)]
		public int FetchSize
		{
			get { return this.fetchSize; }
			set
			{
				if (this.activeReader != null)
				{
					throw new InvalidOperationException("There is already an open DataReader associated with this Command which must be closed first.");
				}
				this.fetchSize = value;
			}
		}

#if (!(NET_35 && !ENTITY_FRAMEWORK))
		// type coercions
		internal Type[] ExpectedColumnTypes { get; private set; }
#endif

		#endregion

		#region � Protected DbCommand Properties �

		protected override DbConnection DbConnection
		{
			get { return this.Connection; }
			set { this.Connection = (FbConnection)value; }
		}

		protected override DbTransaction DbTransaction
		{
			get { return this.Transaction; }
			set { this.Transaction = (FbTransaction)value; }
		}

		protected override DbParameterCollection DbParameterCollection
		{
			get { return this.Parameters; }
		}

		#endregion

		#region � Design-Time properties �

		[Browsable(false)]
		[DesignOnly(true)]
		[DefaultValue(true)]
		public override bool DesignTimeVisible
		{
			get { return this.designTimeVisible; }
			set
			{
				this.designTimeVisible = value;
				TypeDescriptor.Refresh(this);
			}
		}

		#endregion

		#region � Internal Properties �

		internal int RecordsAffected
		{
			get
			{
				if (this.statement != null && this.CommandType != CommandType.StoredProcedure)
				{
					return this.statement.RecordsAffected;
				}
				return -1;
			}
		}

		internal bool IsDisposed
		{
			get { return this.disposed; }
		}

		internal FbDataReader ActiveReader
		{
			get { return this.activeReader; }
			set { this.activeReader = value; }
		}

		internal FbTransaction ActiveTransaction
		{
			get { return this.transaction; }
		}

		internal bool HasImplicitTransaction
		{
			get { return this.implicitTransaction; }
		}

		internal bool IsSelectCommand
		{
			get
			{
				if (this.statement != null)
				{
					if (this.statement.StatementType == DbStatementType.Select ||
						this.statement.StatementType == DbStatementType.SelectForUpdate)
					{
						return true;
					}
				}
				return false;
			}
		}

		internal bool IsDDLCommand
		{
			get { return (this.statement != null && this.statement.StatementType == DbStatementType.DDL); }
		}

		#endregion

		#region � Constructors �

		public FbCommand()
			: this(null, null, null)
		{
		}

		public FbCommand(string cmdText)
			: this(cmdText, null, null)
		{
		}

		public FbCommand(string cmdText, FbConnection connection)
			: this(cmdText, connection, null)
		{
		}

		public FbCommand(string cmdText, FbConnection connection, FbTransaction transaction)
			: base()
		{
			this.namedParameters = new List<string>();
			this.updatedRowSource = UpdateRowSource.Both;
			this.commandType = CommandType.Text;
			this.designTimeVisible = true;
			this.commandTimeout = 30;
			this.fetchSize = 200;
			this.commandText = string.Empty;

			if (connection != null)
			{
				this.fetchSize = connection.ConnectionOptions.FetchSize;
			}

			if (cmdText != null)
			{
				this.CommandText = cmdText;
			}

			this.Connection = connection;
			this.transaction = transaction;
		}

		internal FbCommand(Type[] expectedColumnTypes)
			: this()
		{
			this.ExpectedColumnTypes = expectedColumnTypes;
		}

		#endregion

		#region � IDisposable methods �

		protected override void Dispose(bool disposing)
		{
			lock (this)
			{
				if (!this.disposed)
				{
					try
					{
						// Release any unmanaged resources
						this.Release();

						// release any managed resources
						this.commandTimeout = 0;
						this.fetchSize = 0;
						this.implicitTransaction = false;
						this.commandText = null;
						this.connection = null;
						this.transaction = null;
						this.parameters = null;
						this.statement = null;
						this.activeReader = null;

						if (this.namedParameters != null)
						{
							this.namedParameters.Clear();
							this.namedParameters = null;
						}

						this.disposed = true;
					}
					finally
					{
						base.Dispose(disposing);
					}
				}
			}
		}

		#endregion

		#region � ICloneable Methods �

		object ICloneable.Clone()
		{
			FbCommand command = new FbCommand();

			command.CommandText = this.CommandText;
			command.Connection = this.Connection;
			command.Transaction = this.Transaction;
			command.CommandType = this.CommandType;
			command.UpdatedRowSource = this.UpdatedRowSource;
			command.CommandTimeout = this.CommandTimeout;
			command.FetchSize = this.FetchSize;
			command.UpdatedRowSource = this.UpdatedRowSource;

#if (!(NET_35 && !ENTITY_FRAMEWORK))
			if (this.ExpectedColumnTypes != null)
				command.ExpectedColumnTypes = (Type[])this.ExpectedColumnTypes.Clone();
#endif

			for (int i = 0; i < this.Parameters.Count; i++)
			{
				command.Parameters.Add(((ICloneable)this.Parameters[i]).Clone());
			}

			return command;
		}

		#endregion

		#region � Methods �

		public override void Cancel()
		{
			this.connection.CancelCommand();
		}

		public new FbParameter CreateParameter()
		{
			return new FbParameter();
		}

		public override void Prepare()
		{
			lock (this)
			{
				this.CheckCommand();

				try
				{
					this.Prepare(false);
				}
				catch (IscException ex)
				{
					this.DiscardImplicitTransaction();

					throw new FbException(ex.Message, ex);
				}
				catch
				{
					this.DiscardImplicitTransaction();

					throw;
				}
			}
		}

		public override int ExecuteNonQuery()
		{
			lock (this)
			{
				this.CheckCommand();

				try
				{
					this.ExecuteCommand(CommandBehavior.Default);

					if (this.statement.StatementType == DbStatementType.StoredProcedure)
					{
						this.SetOutputParameters();
					}

					this.CommitImplicitTransaction();
				}
				catch (IscException ex)
				{
					this.DiscardImplicitTransaction();

					throw new FbException(ex.Message, ex);
				}
				catch
				{
					this.DiscardImplicitTransaction();

					throw;
				}
			}

			return this.RecordsAffected;
		}
		public IAsyncResult BeginExecuteNonQuery(AsyncCallback callback, object objectState)
		{
			// BeginInvoke might be slow, but the query processing will make this irrelevant
			return ((Func<int>)this.ExecuteNonQuery).BeginInvoke(callback, objectState);
		}
		public int EndExecuteNonQuery(IAsyncResult asyncResult)
		{
			return ((Func<int>)(asyncResult as AsyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}

		public new FbDataReader ExecuteReader()
		{
			return this.ExecuteReader(CommandBehavior.Default);
		}
		public new FbDataReader ExecuteReader(CommandBehavior behavior)
		{
			lock (this)
			{
				this.CheckCommand();

				try
				{
					this.ExecuteCommand(behavior, true);
				}
				catch (IscException ex)
				{
					this.DiscardImplicitTransaction();

					throw new FbException(ex.Message, ex);
				}
				catch
				{
					this.DiscardImplicitTransaction();

					throw;
				}
			}

			this.activeReader = new FbDataReader(this, this.connection, behavior);

			return this.activeReader;
		}
		public IAsyncResult BeginExecuteReader(AsyncCallback callback, object objectState)
		{
			// BeginInvoke might be slow, but the query processing will make this irrelevant
			return ((Func<FbDataReader>)this.ExecuteReader).BeginInvoke(callback, objectState);
		}
		public IAsyncResult BeginExecuteReader(CommandBehavior behavior, AsyncCallback callback, object objectState)
		{
			// BeginInvoke might be slow, but the query processing will make this irrelevant
			return ((Func<CommandBehavior, FbDataReader>)this.ExecuteReader).BeginInvoke(behavior, callback, objectState);
		}
		public FbDataReader EndExecuteReader(IAsyncResult asyncResult)
		{
			if ((asyncResult as AsyncResult).AsyncDelegate is Func<FbDataReader>)
			{
				return ((Func<FbDataReader>)(asyncResult as AsyncResult).AsyncDelegate).EndInvoke(asyncResult);
			}
			else
			{
				return ((Func<CommandBehavior, FbDataReader>)(asyncResult as AsyncResult).AsyncDelegate).EndInvoke(asyncResult);
			}
		}

		public override object ExecuteScalar()
		{
			DbValue[] values = null;
			object val = null;

			lock (this)
			{
				this.CheckCommand();

				try
				{
					this.ExecuteCommand(CommandBehavior.Default);

					// Gets	only the values	of the first row or
					// the output parameters values if command is an Stored Procedure
					if (this.statement.StatementType == DbStatementType.StoredProcedure)
					{
						values = this.statement.GetOutputParameters();
						this.SetOutputParameters(values);
					}
					else
					{
						values = this.statement.Fetch();
					}

					// Get the return value
					if (values != null && values.Length > 0)
					{
						val = values[0].Value;
					}

					this.CommitImplicitTransaction();
				}
				catch (IscException ex)
				{
					this.DiscardImplicitTransaction();

					throw new FbException(ex.Message, ex);
				}
				catch
				{
					this.DiscardImplicitTransaction();

					throw;
				}
			}

			return val;
		}
		public IAsyncResult BeginExecuteScalar(AsyncCallback callback, object objectState)
		{
			// BeginInvoke might be slow, but the query processing will make this irrelevant
			return ((Func<object>)this.ExecuteScalar).BeginInvoke(callback, objectState);
		}
		public object EndExecuteScalar(IAsyncResult asyncResult)
		{
			return ((Func<object>)(asyncResult as AsyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}

		#endregion

		#region � DbCommand Protected Methods �

		protected override DbParameter CreateDbParameter()
		{
			return this.CreateParameter();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			return this.ExecuteReader(behavior);
		}

		#endregion

		#region � Internal Methods �

		internal void CloseReader()
		{
			if (this.activeReader != null)
			{
				this.activeReader.Close();
				this.activeReader = null;
			}
		}

		internal DbValue[] Fetch()
		{
			try
			{
				if (this.statement != null)
				{
					// Fetch the next row
					return this.statement.Fetch();
				}
			}
			catch (IscException ex)
			{
				throw new FbException(ex.Message, ex);
			}

			return null;
		}

		internal Descriptor GetFieldsDescriptor()
		{
			if (this.statement != null)
			{
				return this.statement.Fields;
			}

			return null;
		}

		internal void SetOutputParameters()
		{
			this.SetOutputParameters(null);
		}

		internal void SetOutputParameters(DbValue[] outputParameterValues)
		{
			if (this.Parameters.Count > 0 && this.statement != null)
			{
				if (this.statement != null &&
					this.statement.StatementType == DbStatementType.StoredProcedure)
				{
					DbValue[] values = outputParameterValues;
					if (outputParameterValues == null)
					{
						values = (DbValue[])this.statement.GetOutputParameters();
					}

					if (values != null && values.Length > 0)
					{
						int i = 0;
						foreach (FbParameter parameter in this.Parameters)
						{
							if (parameter.Direction == ParameterDirection.Output ||
								parameter.Direction == ParameterDirection.InputOutput ||
								parameter.Direction == ParameterDirection.ReturnValue)
							{
								parameter.Value = values[i].Value;
								i++;
							}
						}
					}
				}
			}
		}

		internal void DiscardImplicitTransaction()
		{
			if (this.IsSelectCommand)
			{
				this.CommitImplicitTransaction();
			}
			else
			{
				this.RollbackImplicitTransaction();
			}
		}

		internal void CommitImplicitTransaction()
		{
			if (this.HasImplicitTransaction &&
				this.transaction != null &&
				this.transaction.Transaction != null)
			{
				try
				{
					this.transaction.Commit();
				}
				catch
				{
					this.RollbackImplicitTransaction();

					throw;
				}
				finally
				{
					if (this.transaction != null)
					{
						this.transaction.Dispose();
						this.transaction = null;
						this.implicitTransaction = false;
					}

					if (this.statement != null)
					{
						this.statement.Transaction = null;
					}
				}
			}
		}

		internal void RollbackImplicitTransaction()
		{
			if (this.HasImplicitTransaction && this.transaction != null && this.transaction.Transaction != null)
			{
				int transactionCount = this.Connection.InnerConnection.Database.TransactionCount;

				try
				{
					this.transaction.Rollback();
				}
				catch
				{
					if (this.Connection.InnerConnection.Database.TransactionCount == transactionCount)
					{
						this.Connection.InnerConnection.Database.TransactionCount--;
					}
				}
				finally
				{
					this.transaction.Dispose();
					this.transaction = null;
					this.implicitTransaction = false;

					if (this.statement != null)
					{
						this.statement.Transaction = null;
					}
				}
			}
		}

		internal void Close()
		{
			if (this.statement != null)
			{
				this.statement.Close();
			}
		}

		internal void Release()
		{
			// Rollback implicit transaction
			this.RollbackImplicitTransaction();

			// If there	are	an active reader close it
			this.CloseReader();

			// Remove the command from the Prepared commands list
			if (this.connection != null && this.connection.State == ConnectionState.Open)
			{
				this.connection.InnerConnection.RemovePreparedCommand(this);
			}

			// Dipose the inner statement
			if (this.statement != null)
			{
				this.statement.Dispose();
				this.statement = null;
			}
		}

		#endregion

		#region � Input parameter descriptor generation methods �

		private void DescribeInput()
		{
			if (this.Parameters.Count > 0)
			{
				Descriptor descriptor = this.BuildParametersDescriptor();
				if (descriptor == null)
				{
					this.statement.DescribeParameters();
				}
				else
				{
					this.statement.Parameters = descriptor;
				}
			}
		}

		private Descriptor BuildParametersDescriptor()
		{
			short count = this.ValidateInputParameters();

			if (count > 0)
			{
				if (this.namedParameters.Count > 0)
				{
					count = (short)this.namedParameters.Count;
					return this.BuildNamedParametersDescriptor(count);
				}
				else
				{
					return this.BuildPlaceHoldersDescriptor(count);
				}
			}

			return null;
		}

		private Descriptor BuildNamedParametersDescriptor(short count)
		{
			Descriptor descriptor = new Descriptor(count);
			int index = 0;

			for (int i = 0; i < this.namedParameters.Count; i++)
			{
				if (this.Parameters.IndexOf(this.namedParameters[i]) == -1)
				{
					throw new FbException(String.Format("Must declare the variable '{0}'", this.namedParameters[i]));
				}

				FbParameter parameter = this.Parameters[this.namedParameters[i]];

				if (parameter.Direction == ParameterDirection.Input ||
					parameter.Direction == ParameterDirection.InputOutput)
				{
					if (!this.BuildParameterDescriptor(descriptor, parameter, index++))
					{
						return null;
					}
				}
			}

			return descriptor;
		}

		private Descriptor BuildPlaceHoldersDescriptor(short count)
		{
			Descriptor descriptor = new Descriptor(count);
			int index = 0;

			for (int i = 0; i < this.Parameters.Count; i++)
			{
				FbParameter parameter = this.Parameters[i];

				if (parameter.Direction == ParameterDirection.Input ||
					parameter.Direction == ParameterDirection.InputOutput)
				{
					if (!this.BuildParameterDescriptor(descriptor, parameter, index++))
					{
						return null;
					}
				}
			}

			return descriptor;
		}

		private bool BuildParameterDescriptor(Descriptor descriptor, FbParameter parameter, int index)
		{
			if (!parameter.IsTypeSet)
			{
				return false;
			}

			FbDbType type = parameter.FbDbType;
			Charset charset = this.connection.InnerConnection.Database.Charset;

			// Check the parameter character set
			if (parameter.Charset == FbCharset.Octets && !(parameter.InternalValue is byte[]))
			{
				throw new InvalidOperationException("Value for char octets fields should be a byte array");
			}
			else if (type == FbDbType.Guid)
			{
				charset = Charset.GetCharset("OCTETS");
			}
			else if (parameter.Charset != FbCharset.Default)
			{
				charset = Charset.GetCharset((int)parameter.Charset);
			}

			// Set parameter Data Type
			descriptor[index].DataType = (short)TypeHelper.GetFbType((DbDataType)type, parameter.IsNullable);

			// Set parameter Sub Type
			switch (type)
			{
				case FbDbType.Binary:
					descriptor[index].SubType = 0;
					break;

				case FbDbType.Text:
					descriptor[index].SubType = 1;
					break;

				case FbDbType.Guid:
					descriptor[index].SubType = (short)charset.Identifier;
					break;

				case FbDbType.Char:
				case FbDbType.VarChar:
					descriptor[index].SubType = (short)charset.Identifier;
					if (charset.IsOctetsCharset)
					{
						descriptor[index].Length = (short)parameter.Size;
					}
					else if (parameter.HasSize)
					{
						short len = (short)(parameter.Size * charset.BytesPerCharacter);
						descriptor[index].Length = len;
					}
					break;
			}

			// Set parameter length
			if (descriptor[index].Length == 0)
			{
				descriptor[index].Length = TypeHelper.GetSize((DbDataType)type);
			}

			// Verify parameter
			if (descriptor[index].SqlType == 0 || descriptor[index].Length == 0)
			{
				return false;
			}

			return true;
		}

		private short ValidateInputParameters()
		{
			short count = 0;

			for (int i = 0; i < this.Parameters.Count; i++)
			{
				if (this.Parameters[i].Direction == ParameterDirection.Input ||
					this.Parameters[i].Direction == ParameterDirection.InputOutput)
				{
					FbDbType type = this.Parameters[i].FbDbType;

					if (type == FbDbType.Array || type == FbDbType.Decimal || type == FbDbType.Numeric)
					{
						return -1;
					}
					else
					{
						count++;
					}
				}
			}

			return count;
		}

		private void UpdateParameterValues()
		{
			int index = -1;

			for (int i = 0; i < this.statement.Parameters.Count; i++)
			{
				index = i;

				if (this.namedParameters.Count > 0)
				{
					index = this.Parameters.IndexOf(this.namedParameters[i]);
					if (index == -1)
					{
						throw new FbException(String.Format("Must declare the variable '{0}'", this.namedParameters[i]));
					}
				}

				if (index != -1)
				{
					if (this.Parameters[index].InternalValue == DBNull.Value || this.Parameters[index].InternalValue == null)
					{
						this.statement.Parameters[i].NullFlag = -1;
						this.statement.Parameters[i].Value = DBNull.Value;

						if (!this.statement.Parameters[i].AllowDBNull())
						{
							this.statement.Parameters[i].DataType++;
						}
					}
					else
					{
						// Parameter value is not null
						this.statement.Parameters[i].NullFlag = 0;

						switch (this.statement.Parameters[i].DbDataType)
						{
							case DbDataType.Binary:
								{
									BlobBase blob = this.statement.CreateBlob();
									blob.Write((byte[])this.Parameters[index].InternalValue);
									this.statement.Parameters[i].Value = blob.Id;
								}
								break;

							case DbDataType.Text:
								{
									BlobBase blob = this.statement.CreateBlob();
									if (this.Parameters[index].InternalValue is byte[])
										blob.Write((byte[])this.Parameters[index].InternalValue);
									else
										blob.Write((string)this.Parameters[index].InternalValue);
									this.statement.Parameters[i].Value = blob.Id;
								}
								break;

							case DbDataType.Array:
								{
									if (this.statement.Parameters[i].ArrayHandle == null)
									{
										this.statement.Parameters[i].ArrayHandle =
										this.statement.CreateArray(
											this.statement.Parameters[i].Relation,
											this.statement.Parameters[i].Name);
									}
									else
									{
										this.statement.Parameters[i].ArrayHandle.DB = this.statement.Database;
										this.statement.Parameters[i].ArrayHandle.Transaction = this.statement.Transaction;
									}

									this.statement.Parameters[i].ArrayHandle.Handle = 0;
									this.statement.Parameters[i].ArrayHandle.Write((System.Array)this.Parameters[index].InternalValue);
									this.statement.Parameters[i].Value = this.statement.Parameters[i].ArrayHandle.Handle;
								}
								break;

							case DbDataType.Guid:
								if (!(this.Parameters[index].InternalValue is Guid) &&
									!(this.Parameters[index].InternalValue is byte[]))
								{
									throw new InvalidOperationException("Incorrect Guid value.");
								}
								this.statement.Parameters[i].Value = this.Parameters[index].InternalValue;
								break;

							default:
								this.statement.Parameters[i].Value = this.Parameters[index].InternalValue;
								break;
						}
					}
				}
			}
		}

		#endregion

		#region � Private Methods �

		private void Prepare(bool returnsSet)
		{
			LogCommand();

			FbConnectionInternal innerConn = this.connection.InnerConnection;

			// Check if	we have	a valid	transaction
			if (this.transaction == null)
			{
				if (innerConn.IsEnlisted)
				{
					this.transaction = innerConn.ActiveTransaction;
				}
				else
				{
					this.implicitTransaction = true;
					this.transaction = new FbTransaction(this.connection, this.connection.ConnectionOptions.IsolationLevel);
					this.transaction.BeginTransaction();

					// Update Statement	transaction
					if (this.statement != null)
					{
						this.statement.Transaction = this.transaction.Transaction;
					}
				}
			}

			// Check if	we have	a valid	statement handle
			if (this.statement == null)
			{
				this.statement = innerConn.Database.CreateStatement(this.transaction.Transaction);
			}

			// Prepare the statement if	needed
			if (!this.statement.IsPrepared)
			{
				// Close the inner DataReader if needed
				this.CloseReader();

				// Reformat the SQL statement if needed
				string sql = this.commandText;

				if (this.commandType == CommandType.StoredProcedure)
				{
					sql = this.BuildStoredProcedureSql(sql, returnsSet);
				}

				try
				{
					// Try to prepare the command
					this.statement.Prepare(this.ParseNamedParameters(sql));
				}
				catch
				{
					// Release the statement and rethrow the exception
					this.statement.Release();
					this.statement = null;

					throw;
				}

				// Add this	command	to the active command list
				innerConn.AddPreparedCommand(this);
			}
			else
			{
				// Close statement for subsequently	executions
				this.Close();
			}
		}

		private void ExecuteCommand(CommandBehavior behavior)
		{
			this.ExecuteCommand(behavior, false);
		}

		private void ExecuteCommand(CommandBehavior behavior, bool returnsSet)
		{
			// Prepare statement
			this.Prepare(returnsSet);

			if ((behavior & CommandBehavior.SequentialAccess) == CommandBehavior.SequentialAccess ||
				(behavior & CommandBehavior.SingleResult) == CommandBehavior.SingleResult ||
				(behavior & CommandBehavior.SingleRow) == CommandBehavior.SingleRow ||
				(behavior & CommandBehavior.CloseConnection) == CommandBehavior.CloseConnection ||
				behavior == CommandBehavior.Default)
			{
				// Set the fetch size
				this.statement.FetchSize = this.fetchSize;

				// Set if it's needed the Records Affected information
				this.statement.ReturnRecordsAffected = this.connection.ConnectionOptions.ReturnRecordsAffected;

				// Validate input parameter count
				if (this.namedParameters.Count > 0 && this.Parameters.Count == 0)
				{
					throw new FbException("Must declare command parameters.");
				}

				// Update input parameter values
				if (this.Parameters.Count > 0)
				{
					if (this.statement.Parameters == null)
					{
						this.DescribeInput();
					}
					this.UpdateParameterValues();
				}

				// Execute statement
				this.statement.Execute();
			}
		}

		private string BuildStoredProcedureSql(string spName, bool returnsSet)
		{
			string sql = spName == null ? string.Empty : spName.Trim();

			if (sql.Length > 0 &&
				!sql.ToLower(CultureInfo.InvariantCulture).StartsWith("execute procedure ") &&
				!sql.ToLower(CultureInfo.InvariantCulture).StartsWith("select "))
			{
				StringBuilder paramsText = new StringBuilder();

				// Append the stored proc parameter	name
				paramsText.Append(sql);
				if (this.Parameters.Count > 0)
				{
					paramsText.Append("(");
					for (int i = 0; i < this.Parameters.Count; i++)
					{
						if (this.Parameters[i].Direction == ParameterDirection.Input ||
							this.Parameters[i].Direction == ParameterDirection.InputOutput)
						{
							// Append parameter	name to parameter list
							paramsText.Append(this.Parameters[i].InternalParameterName);
							if (i != this.Parameters.Count - 1)
							{
								paramsText = paramsText.Append(",");
							}
						}
					}
					paramsText.Append(")");
					paramsText.Replace(",)", ")");
					paramsText.Replace("()", string.Empty);
				}

				if (returnsSet)
				{
					sql = "select * from " + paramsText.ToString();
				}
				else
				{
					sql = "execute procedure " + paramsText.ToString();
				}
			}

			return sql;
		}

		private string ParseNamedParameters(string sql)
		{
			StringBuilder builder = new StringBuilder();
			StringBuilder paramBuilder = new StringBuilder();
			bool inSingleQuotes = false;
			bool inDoubleQuotes = false;
			bool inParam = false;

			this.namedParameters.Clear();

			if (sql.IndexOf('@') == -1)
			{
				return sql;
			}

			for (int i = 0; i < sql.Length; i++)
			{
				char sym = sql[i];

				if (inParam)
				{
					if (Char.IsLetterOrDigit(sym) || sym == '_' || sym == '$')
					{
						paramBuilder.Append(sym);
					}
					else
					{
						this.namedParameters.Add(paramBuilder.ToString());
						paramBuilder.Length = 0;
						builder.Append('?');
						builder.Append(sym);
						inParam = false;
					}
				}
				else
				{
					if (sym == '\'' && !inDoubleQuotes)
					{
						inSingleQuotes = !inSingleQuotes;
					}
					else if (sym == '\"' && !inSingleQuotes)
					{
						inDoubleQuotes = !inDoubleQuotes;
					}
					else if (!(inSingleQuotes || inDoubleQuotes) && sym == '@')
					{
						inParam = true;
						paramBuilder.Append(sym);
						continue;
					}

					builder.Append(sym);
				}
			}

			if (inParam)
			{
				this.namedParameters.Add(paramBuilder.ToString());
				builder.Append('?');
			}

			return builder.ToString();
		}

		private void CheckCommand()
		{
			if (this.transaction != null && this.transaction.IsUpdated)
			{
				this.transaction = null;
			}

			if (this.connection == null ||
				this.connection.State != ConnectionState.Open)
			{
				throw new InvalidOperationException("Connection must be valid and open");
			}

			if (this.activeReader != null)
			{
				throw new InvalidOperationException("There is already an open DataReader associated with this Command which must be closed first.");
			}

			if (this.transaction == null &&
				this.connection.InnerConnection.HasActiveTransaction &&
				!this.connection.InnerConnection.IsEnlisted)
			{
				throw new InvalidOperationException("Execute requires the Command object to have a Transaction object when the Connection object assigned to the command is in a pending local transaction. The Transaction property of the Command has not been initialized.");
			}

			if (this.transaction != null && !this.transaction.IsUpdated &&
				!this.connection.Equals(transaction.Connection))
			{
				throw new InvalidOperationException("Command Connection is not equal to Transaction Connection.");
			}

			if (this.commandText == null || this.commandText.Length == 0)
			{
				throw new InvalidOperationException("The command text for this Command has not been set.");
			}
		}

		[Conditional(TraceHelper.ConditionalSymbol)]
		private void LogCommand()
		{
			StringBuilder message = new StringBuilder();
			message.AppendLine("Command:");
			message.AppendLine(commandText);
			message.AppendLine("Parameters:");
			if (this.parameters != null)
			{
				foreach (FbParameter item in this.parameters)
				{
					message.AppendLine(string.Format("Name:{0}\tType:{1}\tUsed Value:{2}", item.ParameterName, item.FbDbType, (!IsNullParameterValue(item.InternalValue) ? item.InternalValue : "<null>")));
				}
			}
			else
			{
				message.AppendLine("<no parameters>");
			}
			TraceHelper.Trace(TraceEventType.Information, message.ToString());
		}

		private bool IsNullParameterValue(object value)
		{
			return (value == DBNull.Value || value == null);
		}

		#endregion
	}
}
