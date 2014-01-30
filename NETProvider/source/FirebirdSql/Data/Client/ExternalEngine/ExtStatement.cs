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
 *	Copyright (c) 2005 Carlos Guzman Alvarez
 *	All Rights Reserved.
 */

using System;
using System.Collections;
using System.Text;

using FirebirdSql.Data.Common;
using FirebirdSql.Data.Client.Common;

namespace FirebirdSql.Data.Client.ExternalEngine
{
	internal sealed class ExtStatement : StatementBase
	{
		#region � Fields �

		private int handle;
		private ExtDatabase db;
		private ExtTransaction transaction;
		private Descriptor parameters;
		private Descriptor fields;
		private StatementState state;
		private DbStatementType statementType;
		private bool allRowsFetched;
		private Queue outputParams;
		private int recordsAffected;
		private bool returnRecordsAffected;

		#endregion

		#region � Properties �

		public override IDatabase Database
		{
			get { return this.db; }
		}

		public override ITransaction Transaction
		{
			get { return this.transaction; }
			set
			{
				if (this.transaction != value)
				{
					if (this.TransactionUpdate != null && this.transaction != null)
					{
						this.transaction.Update -= this.TransactionUpdate;
						this.TransactionUpdate = null;
					}

					if (value == null)
					{
						this.transaction = null;
					}
					else
					{
						this.transaction = (ExtTransaction)value;
						this.TransactionUpdate = new TransactionUpdateEventHandler(this.TransactionUpdated);
						this.transaction.Update += this.TransactionUpdate;
					}
				}
			}
		}

		public override Descriptor Parameters
		{
			get { return this.parameters; }
			set { this.parameters = value; }
		}

		public override Descriptor Fields
		{
			get { return this.fields; }
		}

		public override int RecordsAffected
		{
			get { return this.recordsAffected; }
			protected set { this.recordsAffected = value; }
		}

		public override bool IsPrepared
		{
			get
			{
				if (this.state == StatementState.Deallocated || this.state == StatementState.Error)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		public override DbStatementType StatementType
		{
			get { return this.statementType; }
			protected set { this.statementType = value; }
		}

		public override StatementState State
		{
			get { return this.state; }
			protected set { this.state = value; }
		}

		public override int FetchSize
		{
			get { return 200; }
			set { ;	}
		}

		public override bool ReturnRecordsAffected
		{
			get { return this.returnRecordsAffected; }
			set { this.returnRecordsAffected = value; }
		}

		#endregion

		#region � Constructors �

		public ExtStatement(IDatabase db)
			: this(db, null)
		{
		}

		public ExtStatement(IDatabase db, ITransaction transaction)
		{
			if (!(db is ExtDatabase))
			{
				throw new ArgumentException("Specified argument is not of FesDatabase type.");
			}

			this.recordsAffected = -1;
			this.db = (ExtDatabase)db;
			this.outputParams = new Queue();

			if (transaction != null)
			{
				this.Transaction = transaction;
			}

			GC.SuppressFinalize(this);
		}

		#endregion

		#region � IDisposable methods �

		protected override void Dispose(bool disposing)
		{
			if (!this.IsDisposed)
			{
				try
				{
					// release any unmanaged resources
					this.Release();

					// release any managed resources
					if (disposing)
					{
						this.Clear();

						this.db = null;
						this.fields = null;
						this.parameters = null;
						this.transaction = null;
						this.outputParams = null;
						this.allRowsFetched = false;
						this.recordsAffected = 0;
						this.state = StatementState.Deallocated;
						this.handle = 0;
					}
				}
				finally
				{
					base.Dispose(disposing);
				}
			}
		}

		#endregion

		#region � Blob Creation Metods �

		public override BlobBase CreateBlob()
		{
			return new ExtBlob(this.db, this.transaction);
		}

		public override BlobBase CreateBlob(long blobId)
		{
			return new ExtBlob(this.db, this.transaction, blobId);
		}

		#endregion

		#region � Array Creation Methods �

		public override ArrayBase CreateArray(ArrayDesc descriptor)
		{
			return new ExtArray(descriptor);
		}

		public override ArrayBase CreateArray(string tableName, string fieldName)
		{
			return new ExtArray(this.db, this.transaction, tableName, fieldName);
		}

		public override ArrayBase CreateArray(long handle, string tableName, string fieldName)
		{
			return new ExtArray(this.db, this.transaction, handle, tableName, fieldName);
		}

		#endregion

		#region � Methods �

		public override void Prepare(string commandText)
		{
			// Clear data
			this.ClearAll();

			lock (this.db)
			{
				if (this.state == StatementState.Deallocated)
				{
					// Allocate	statement
					this.Allocate();
				}

				// Marshal structures to pointer
				XsqldaMarshaler marshaler = XsqldaMarshaler.Instance;

				// Setup fields	structure
				this.fields = new Descriptor(1);

				IntPtr sqlda = marshaler.MarshalManagedToNative(this.db.Charset, this.fields);

				int[] statusVector = ExtConnection.GetNewStatusVector();
				int trHandle = this.transaction.Handle;
				int stmtHandle = this.handle;

				byte[] buffer = this.db.Charset.GetBytes(commandText);

				SafeNativeMethods.isc_dsql_prepare(
					statusVector,
					ref	trHandle,
					ref	stmtHandle,
					(short)buffer.Length,
					buffer,
					this.db.Dialect,
					sqlda);

				// Marshal Pointer
				Descriptor descriptor = marshaler.MarshalNativeToManaged(this.db.Charset, sqlda);

				// Free	memory
				marshaler.CleanUpNativeData(ref	sqlda);

				// Parse status	vector
				this.db.ParseStatusVector(statusVector);

				// Describe	fields
				this.fields = descriptor;
				if (this.fields.ActualCount > 0 && this.fields.ActualCount != this.fields.Count)
				{
					this.Describe();
				}
				else
				{
					if (this.fields.ActualCount == 0)
					{
						this.fields = new Descriptor(0);
					}
				}

				// Reset actual	field values
				this.fields.ResetValues();

				// Get Statement type
				this.statementType = this.GetStatementType();

				// Update state
				this.state = StatementState.Prepared;
			}
		}

		public override void Execute()
		{
			if (this.state == StatementState.Deallocated)
			{
				throw new InvalidOperationException("Statment is not correctly created.");
			}

			lock (this.db)
			{
				// Marshal structures to pointer
				XsqldaMarshaler marshaler = XsqldaMarshaler.Instance;

				IntPtr inSqlda = IntPtr.Zero;
				IntPtr outSqlda = IntPtr.Zero;

				if (this.parameters != null)
				{
					inSqlda = marshaler.MarshalManagedToNative(this.db.Charset, this.parameters);
				}
				if (this.statementType == DbStatementType.StoredProcedure)
				{
					this.Fields.ResetValues();
					outSqlda = marshaler.MarshalManagedToNative(this.db.Charset, this.fields);
				}

				int[] statusVector = ExtConnection.GetNewStatusVector();
				int trHandle = this.transaction.Handle;
				int stmtHandle = this.handle;

				SafeNativeMethods.isc_dsql_execute2(
					statusVector,
					ref	trHandle,
					ref	stmtHandle,
					IscCodes.SQLDA_VERSION1,
					inSqlda,
					outSqlda);

				if (outSqlda != IntPtr.Zero)
				{
					Descriptor descriptor = marshaler.MarshalNativeToManaged(this.db.Charset, outSqlda);

					// This	would be an	Execute	procedure
					DbValue[] values = new DbValue[descriptor.Count];

					for (int i = 0; i < values.Length; i++)
					{
						values[i] = new DbValue(this, descriptor[i]);
					}

					this.outputParams.Enqueue(values);
				}

				// Free	memory
				marshaler.CleanUpNativeData(ref	inSqlda);
				marshaler.CleanUpNativeData(ref	outSqlda);

				this.db.ParseStatusVector(statusVector);

				this.UpdateRecordsAffected();

				this.state = StatementState.Executed;
			}
		}

		public override DbValue[] Fetch()
		{
			DbValue[] row = null;

			if (this.state == StatementState.Deallocated)
			{
				throw new InvalidOperationException("Statement is not correctly created.");
			}
			if (this.statementType != DbStatementType.Select &&
				this.statementType != DbStatementType.SelectForUpdate)
			{
				return null;
			}

			lock (this.db)
			{
				if (!this.allRowsFetched)
				{
					// Get the XSQLDA Marshaler
					XsqldaMarshaler marshaler = XsqldaMarshaler.Instance;

					// Reset actual	field values
					this.fields.ResetValues();

					// Marshal structures to pointer
					IntPtr sqlda = marshaler.MarshalManagedToNative(this.db.Charset, fields);

					// Creta a new status vector
					int[] statusVector = ExtConnection.GetNewStatusVector();

					// Statement handle to be passed to the fetch method
					int stmtHandle = this.handle;

					// Fetch data
					int status = SafeNativeMethods.isc_dsql_fetch(statusVector, ref stmtHandle, IscCodes.SQLDA_VERSION1, sqlda);

					// Obtain values
					Descriptor rowDesc = marshaler.MarshalNativeToManaged(this.db.Charset, sqlda);

					if (this.fields.Count == rowDesc.Count)
					{
						// Try to preserve Array Handle information
						for (int i = 0; i < this.fields.Count; i++)
						{
							if (this.fields[i].IsArray() && this.fields[i].ArrayHandle != null)
							{
								rowDesc[i].ArrayHandle = this.fields[i].ArrayHandle;
							}
						}
					}

					this.fields = rowDesc;

					// Free	memory
					marshaler.CleanUpNativeData(ref	sqlda);

					// Parse status	vector
					this.db.ParseStatusVector(statusVector);

					if (status == 100)
					{
						this.allRowsFetched = true;
					}
					else
					{
						// Set row values
						row = new DbValue[this.fields.ActualCount];
						for (int i = 0; i < row.Length; i++)
						{
							row[i] = new DbValue(this, this.fields[i]);
						}
					}
				}
			}

			return row;
		}

		public override DbValue[] GetOutputParameters()
		{
			if (this.outputParams != null && this.outputParams.Count > 0)
			{
				return (DbValue[])this.outputParams.Dequeue();
			}

			return null;
		}

		public override void Describe()
		{
			lock (this.db)
			{
				// Update structure
				this.fields = new Descriptor(this.fields.ActualCount);

				// Marshal structures to pointer
				XsqldaMarshaler marshaler = XsqldaMarshaler.Instance;

				IntPtr sqlda = marshaler.MarshalManagedToNative(this.db.Charset, fields);

				int[] statusVector = ExtConnection.GetNewStatusVector();
				int stmtHandle = this.handle;

				SafeNativeMethods.isc_dsql_describe(
					statusVector,
					ref	stmtHandle,
					IscCodes.SQLDA_VERSION1,
					sqlda);

				// Marshal Pointer
				Descriptor descriptor = marshaler.MarshalNativeToManaged(this.db.Charset, sqlda);

				// Free	memory
				marshaler.CleanUpNativeData(ref	sqlda);

				// Parse status	vector
				this.db.ParseStatusVector(statusVector);

				// Update field	descriptor
				this.fields = descriptor;
			}
		}

		public override void DescribeParameters()
		{
			lock (this.db)
			{
				// Marshal structures to pointer
				XsqldaMarshaler marshaler = XsqldaMarshaler.Instance;

				this.parameters = new Descriptor(1);

				IntPtr sqlda = marshaler.MarshalManagedToNative(this.db.Charset, parameters);

				int[] statusVector = ExtConnection.GetNewStatusVector();
				int stmtHandle = this.handle;

				SafeNativeMethods.isc_dsql_describe_bind(
					statusVector,
					ref	stmtHandle,
					IscCodes.SQLDA_VERSION1,
					sqlda);

				Descriptor descriptor = marshaler.MarshalNativeToManaged(this.db.Charset, sqlda);

				// Parse status	vector
				this.db.ParseStatusVector(statusVector);

				if (descriptor.ActualCount != 0 && descriptor.Count != descriptor.ActualCount)
				{
					short n = descriptor.ActualCount;
					descriptor = new Descriptor(n);

					// Fre memory
					marshaler.CleanUpNativeData(ref	sqlda);

					// Marshal new structure
					sqlda = marshaler.MarshalManagedToNative(this.db.Charset, descriptor);

					SafeNativeMethods.isc_dsql_describe_bind(
						statusVector,
						ref	stmtHandle,
						IscCodes.SQLDA_VERSION1,
						sqlda);

					descriptor = marshaler.MarshalNativeToManaged(this.db.Charset, sqlda);

					// Free	memory
					marshaler.CleanUpNativeData(ref	sqlda);

					// Parse status	vector
					this.db.ParseStatusVector(statusVector);
				}
				else
				{
					if (descriptor.ActualCount == 0)
					{
						descriptor = new Descriptor(0);
					}
				}

				// Free	memory
				if (sqlda != IntPtr.Zero)
				{
					marshaler.CleanUpNativeData(ref	sqlda);
				}

				// Update parameter	descriptor
				this.parameters = descriptor;
			}
		}

		#endregion

		#region � Protected Methods �

		protected override void Free(int option)
		{
			// Does	not	seem to	be possible	or necessary to	close
			// an execute procedure	statement.
			if (this.StatementType == DbStatementType.StoredProcedure && option == IscCodes.DSQL_close)
			{
				return;
			}

			lock (this.db)
			{
				int[] statusVector = ExtConnection.GetNewStatusVector();
				int stmtHandle = this.handle;

				SafeNativeMethods.isc_dsql_free_statement(
					statusVector,
					ref	stmtHandle,
					(short)option);

				this.handle = stmtHandle;

				// Reset statement information
				if (option == IscCodes.DSQL_drop)
				{
					this.parameters = null;
					this.fields = null;
				}

				this.Clear();
				this.allRowsFetched = false;

				this.db.ParseStatusVector(statusVector);
			}
		}

		protected override void TransactionUpdated(object sender, EventArgs e)
		{
			lock (this)
			{
				if (this.Transaction != null && this.TransactionUpdate != null)
				{
					this.Transaction.Update -= this.TransactionUpdate;
				}
				this.Clear();
				this.State = StatementState.Closed;
				this.TransactionUpdate = null;
				this.allRowsFetched = false;
			}
		}

		protected override byte[] GetSqlInfo(byte[] items, int bufferLength)
		{
			lock (this.db)
			{
				byte[] buffer = new byte[bufferLength];
				int[] statusVector = ExtConnection.GetNewStatusVector();
				int stmtHandle = this.handle;

				SafeNativeMethods.isc_dsql_sql_info(
					statusVector,
					ref	stmtHandle,
					(short)items.Length,
					items,
					(short)bufferLength,
					buffer);

				this.db.ParseStatusVector(statusVector);

				return buffer;
			}
		}

		#endregion

		#region � Private Methods �

		private void Clear()
		{
			if (this.outputParams != null && this.outputParams.Count > 0)
			{
				this.outputParams.Clear();
			}
		}

		private void ClearAll()
		{
			this.Clear();

			this.parameters = null;
			this.fields = null;
		}

		private void Allocate()
		{
			lock (this.db)
			{
				int[] statusVector = ExtConnection.GetNewStatusVector();
				int dbHandle = this.db.Handle;
				int stmtHandle = this.handle;

				SafeNativeMethods.isc_dsql_allocate_statement(
					statusVector,
					ref	dbHandle,
					ref	stmtHandle);

				this.db.ParseStatusVector(statusVector);

				this.handle = stmtHandle;
				this.allRowsFetched = false;
				this.state = StatementState.Allocated;
				this.statementType = DbStatementType.None;
			}
		}

		private void UpdateRecordsAffected()
		{
			if (this.ReturnRecordsAffected &&
				(this.StatementType == DbStatementType.Insert ||
				this.StatementType == DbStatementType.Delete ||
				this.StatementType == DbStatementType.Update ||
				this.StatementType == DbStatementType.StoredProcedure))
			{
				this.recordsAffected = this.GetRecordsAffected();
			}
			else
			{
				this.recordsAffected = -1;
			}
		}

		#endregion
	}
}
