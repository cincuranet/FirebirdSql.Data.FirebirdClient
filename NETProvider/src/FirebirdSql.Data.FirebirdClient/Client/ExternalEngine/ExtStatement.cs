﻿/*
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
		#region Fields

		private int _handle;
		private ExtDatabase _db;
		private ExtTransaction _transaction;
		private Descriptor _parameters;
		private Descriptor _fields;
		private StatementState _state;
		private DbStatementType _statementType;
		private bool _allRowsFetched;
		private Queue _outputParams;
		private int _recordsAffected;
		private bool _returnRecordsAffected;

		#endregion

		#region Properties

		public override IDatabase Database
		{
			get { return _db; }
		}

		public override ITransaction Transaction
		{
			get { return _transaction; }
			set
			{
				if (_transaction != value)
				{
					if (_TransactionUpdate != null && _transaction != null)
					{
						_transaction.Update -= _TransactionUpdate;
						_TransactionUpdate = null;
					}

					if (value == null)
					{
						_transaction = null;
					}
					else
					{
						_transaction = (ExtTransaction)value;
						_TransactionUpdate = new TransactionUpdateEventHandler(TransactionUpdated);
						_transaction.Update += _TransactionUpdate;
					}
				}
			}
		}

		public override Descriptor Parameters
		{
			get { return _parameters; }
			set { _parameters = value; }
		}

		public override Descriptor Fields
		{
			get { return _fields; }
		}

		public override int RecordsAffected
		{
			get { return _recordsAffected; }
			protected set { _recordsAffected = value; }
		}

		public override bool IsPrepared
		{
			get
			{
				if (_state == StatementState.Deallocated || _state == StatementState.Error)
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
			get { return _statementType; }
			protected set { _statementType = value; }
		}

		public override StatementState State
		{
			get { return _state; }
			protected set { _state = value; }
		}

		public override int FetchSize
		{
			get { return 200; }
			set { }
		}

		public override bool ReturnRecordsAffected
		{
			get { return _returnRecordsAffected; }
			set { _returnRecordsAffected = value; }
		}

		#endregion

		#region Constructors

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

			_recordsAffected = -1;
			_db = (ExtDatabase)db;
			_outputParams = new Queue();

			if (transaction != null)
			{
				Transaction = transaction;
			}
		}

		#endregion

		#region IDisposable methods

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				try
				{
					Release();
				}
				catch
				{ }

					if (disposing)
					{
						Clear();

						_db = null;
						_fields = null;
						_parameters = null;
						_transaction = null;
						_outputParams = null;
						_allRowsFetched = false;
						_recordsAffected = 0;
						_state = StatementState.Deallocated;
						_handle = 0;
					}

					base.Dispose(disposing);
				}
			}

		#endregion

		#region Blob Creation Metods

		public override BlobBase CreateBlob()
		{
			return new ExtBlob(_db, _transaction);
		}

		public override BlobBase CreateBlob(long blobId)
		{
			return new ExtBlob(_db, _transaction, blobId);
		}

		#endregion

		#region Array Creation Methods

		public override ArrayBase CreateArray(ArrayDesc descriptor)
		{
			return new ExtArray(descriptor);
		}

		public override ArrayBase CreateArray(string tableName, string fieldName)
		{
			return new ExtArray(_db, _transaction, tableName, fieldName);
		}

		public override ArrayBase CreateArray(long handle, string tableName, string fieldName)
		{
			return new ExtArray(_db, _transaction, handle, tableName, fieldName);
		}

		#endregion

		#region Methods

		public override void Prepare(string commandText)
		{
			// Clear data
			ClearAll();

			lock (_db)
			{
				if (_state == StatementState.Deallocated)
				{
					// Allocate	statement
					Allocate();
				}

				// Setup fields	structure
				_fields = new Descriptor(1);

				IntPtr sqlda = XsqldaMarshaler.MarshalManagedToNative(_db.Charset, _fields);

				int[] statusVector = ExtConnection.GetNewStatusVector();
				int trHandle = _transaction.Handle;
				int stmtHandle = _handle;

				byte[] buffer = _db.Charset.GetBytes(commandText);

				SafeNativeMethods.isc_dsql_prepare(
					statusVector,
					ref	trHandle,
					ref	stmtHandle,
					(short)buffer.Length,
					buffer,
					_db.Dialect,
					sqlda);

				// Marshal Pointer
				Descriptor descriptor = XsqldaMarshaler.MarshalNativeToManaged(_db.Charset, sqlda);

				// Free	memory
				XsqldaMarshaler.CleanUpNativeData(ref	sqlda);

				// Parse status	vector
				_db.ParseStatusVector(statusVector);

				// Describe	fields
				_fields = descriptor;
				if (_fields.ActualCount > 0 && _fields.ActualCount != _fields.Count)
				{
					Describe();
				}
				else
				{
					if (_fields.ActualCount == 0)
					{
						_fields = new Descriptor(0);
					}
				}

				// Reset actual	field values
				_fields.ResetValues();

				// Get Statement type
				_statementType = GetStatementType();

				// Update state
				_state = StatementState.Prepared;
			}
		}

		public override void Execute()
		{
			if (_state == StatementState.Deallocated)
			{
				throw new InvalidOperationException("Statment is not correctly created.");
			}

			lock (_db)
			{

				IntPtr inSqlda = IntPtr.Zero;
				IntPtr outSqlda = IntPtr.Zero;

				if (_parameters != null)
				{
					inSqlda = XsqldaMarshaler.MarshalManagedToNative(_db.Charset, _parameters);
				}
				if (_statementType == DbStatementType.StoredProcedure)
				{
					Fields.ResetValues();
					outSqlda = XsqldaMarshaler.MarshalManagedToNative(_db.Charset, _fields);
				}

				int[] statusVector = ExtConnection.GetNewStatusVector();
				int trHandle = _transaction.Handle;
				int stmtHandle = _handle;

				SafeNativeMethods.isc_dsql_execute2(
					statusVector,
					ref	trHandle,
					ref	stmtHandle,
					IscCodes.SQLDA_VERSION1,
					inSqlda,
					outSqlda);

				if (outSqlda != IntPtr.Zero)
				{
					Descriptor descriptor = XsqldaMarshaler.MarshalNativeToManaged(_db.Charset, outSqlda);

					// This	would be an	Execute	procedure
					DbValue[] values = new DbValue[descriptor.Count];

					for (int i = 0; i < values.Length; i++)
					{
						values[i] = new DbValue(this, descriptor[i]);
					}

					_outputParams.Enqueue(values);
				}

				// Free	memory
				XsqldaMarshaler.CleanUpNativeData(ref	inSqlda);
				XsqldaMarshaler.CleanUpNativeData(ref	outSqlda);

				_db.ParseStatusVector(statusVector);

				UpdateRecordsAffected();

				_state = StatementState.Executed;
			}
		}

		public override DbValue[] Fetch()
		{
			DbValue[] row = null;

			if (_state == StatementState.Deallocated)
			{
				throw new InvalidOperationException("Statement is not correctly created.");
			}
			if (_statementType != DbStatementType.Select &&
				_statementType != DbStatementType.SelectForUpdate)
			{
				return null;
			}

			lock (_db)
			{
				if (!_allRowsFetched)
				{

					// Reset actual	field values
					_fields.ResetValues();

					// Marshal structures to pointer
					IntPtr sqlda = XsqldaMarshaler.MarshalManagedToNative(_db.Charset, _fields);

					// Creta a new status vector
					int[] statusVector = ExtConnection.GetNewStatusVector();

					// Statement handle to be passed to the fetch method
					int stmtHandle = _handle;

					// Fetch data
					int status = SafeNativeMethods.isc_dsql_fetch(statusVector, ref stmtHandle, IscCodes.SQLDA_VERSION1, sqlda);

					// Obtain values
					Descriptor rowDesc = XsqldaMarshaler.MarshalNativeToManaged(_db.Charset, sqlda);

					if (_fields.Count == rowDesc.Count)
					{
						// Try to preserve Array Handle information
						for (int i = 0; i < _fields.Count; i++)
						{
							if (_fields[i].IsArray() && _fields[i].ArrayHandle != null)
							{
								rowDesc[i].ArrayHandle = _fields[i].ArrayHandle;
							}
						}
					}

					_fields = rowDesc;

					// Free	memory
					XsqldaMarshaler.CleanUpNativeData(ref	sqlda);

					// Parse status	vector
					_db.ParseStatusVector(statusVector);

					if (status == 100)
					{
						_allRowsFetched = true;
					}
					else
					{
						// Set row values
						row = new DbValue[_fields.ActualCount];
						for (int i = 0; i < row.Length; i++)
						{
							row[i] = new DbValue(this, _fields[i]);
						}
					}
				}
			}

			return row;
		}

		public override DbValue[] GetOutputParameters()
		{
			if (_outputParams != null && _outputParams.Count > 0)
			{
				return (DbValue[])_outputParams.Dequeue();
			}

			return null;
		}

		public override void Describe()
		{
			lock (_db)
			{
				// Update structure
				_fields = new Descriptor(_fields.ActualCount);

				// Marshal structures to pointer


				IntPtr sqlda = XsqldaMarshaler.MarshalManagedToNative(_db.Charset, _fields);

				int[] statusVector = ExtConnection.GetNewStatusVector();
				int stmtHandle = _handle;

				SafeNativeMethods.isc_dsql_describe(
					statusVector,
					ref	stmtHandle,
					IscCodes.SQLDA_VERSION1,
					sqlda);

				// Marshal Pointer
				Descriptor descriptor = XsqldaMarshaler.MarshalNativeToManaged(_db.Charset, sqlda);

				// Free	memory
				XsqldaMarshaler.CleanUpNativeData(ref	sqlda);

				// Parse status	vector
				_db.ParseStatusVector(statusVector);

				// Update field	descriptor
				_fields = descriptor;
			}
		}

		public override void DescribeParameters()
		{
			lock (_db)
			{
				// Marshal structures to pointer


				_parameters = new Descriptor(1);

				IntPtr sqlda = XsqldaMarshaler.MarshalManagedToNative(_db.Charset, _parameters);

				int[] statusVector = ExtConnection.GetNewStatusVector();
				int stmtHandle = _handle;

				SafeNativeMethods.isc_dsql_describe_bind(
					statusVector,
					ref	stmtHandle,
					IscCodes.SQLDA_VERSION1,
					sqlda);

				Descriptor descriptor = XsqldaMarshaler.MarshalNativeToManaged(_db.Charset, sqlda);

				// Parse status	vector
				_db.ParseStatusVector(statusVector);

				if (descriptor.ActualCount != 0 && descriptor.Count != descriptor.ActualCount)
				{
					short n = descriptor.ActualCount;
					descriptor = new Descriptor(n);

					// Fre memory
					XsqldaMarshaler.CleanUpNativeData(ref	sqlda);

					// Marshal new structure
					sqlda = XsqldaMarshaler.MarshalManagedToNative(_db.Charset, descriptor);

					SafeNativeMethods.isc_dsql_describe_bind(
						statusVector,
						ref	stmtHandle,
						IscCodes.SQLDA_VERSION1,
						sqlda);

					descriptor = XsqldaMarshaler.MarshalNativeToManaged(_db.Charset, sqlda);

					// Free	memory
					XsqldaMarshaler.CleanUpNativeData(ref	sqlda);

					// Parse status	vector
					_db.ParseStatusVector(statusVector);
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
					XsqldaMarshaler.CleanUpNativeData(ref	sqlda);
				}

				// Update parameter	descriptor
				_parameters = descriptor;
			}
		}

		#endregion

		#region Protected Methods

		protected override void Free(int option)
		{
			// Does	not	seem to	be possible	or necessary to	close
			// an execute procedure	statement.
			if (StatementType == DbStatementType.StoredProcedure && option == IscCodes.DSQL_close)
			{
				return;
			}

			lock (_db)
			{
				int[] statusVector = ExtConnection.GetNewStatusVector();
				int stmtHandle = _handle;

				SafeNativeMethods.isc_dsql_free_statement(
					statusVector,
					ref	stmtHandle,
					(short)option);

				_handle = stmtHandle;

				// Reset statement information
				if (option == IscCodes.DSQL_drop)
				{
					_parameters = null;
					_fields = null;
				}

				Clear();
				_allRowsFetched = false;

				_db.ParseStatusVector(statusVector);
			}
		}

		protected override void TransactionUpdated(object sender, EventArgs e)
		{
			lock (this)
			{
				if (Transaction != null && _TransactionUpdate != null)
				{
					Transaction.Update -= _TransactionUpdate;
				}
				Clear();
				State = StatementState.Closed;
				_TransactionUpdate = null;
				_allRowsFetched = false;
			}
		}

		protected override byte[] GetSqlInfo(byte[] items, int bufferLength)
		{
			lock (_db)
			{
				byte[] buffer = new byte[bufferLength];
				int[] statusVector = ExtConnection.GetNewStatusVector();
				int stmtHandle = _handle;

				SafeNativeMethods.isc_dsql_sql_info(
					statusVector,
					ref	stmtHandle,
					(short)items.Length,
					items,
					(short)bufferLength,
					buffer);

				_db.ParseStatusVector(statusVector);

				return buffer;
			}
		}

		#endregion

		#region Private Methods

		private void Clear()
		{
			if (_outputParams != null && _outputParams.Count > 0)
			{
				_outputParams.Clear();
			}
		}

		private void ClearAll()
		{
			Clear();

			_parameters = null;
			_fields = null;
		}

		private void Allocate()
		{
			lock (_db)
			{
				int[] statusVector = ExtConnection.GetNewStatusVector();
				int dbHandle = _db.Handle;
				int stmtHandle = _handle;

				SafeNativeMethods.isc_dsql_allocate_statement(
					statusVector,
					ref	dbHandle,
					ref	stmtHandle);

				_db.ParseStatusVector(statusVector);

				_handle = stmtHandle;
				_allRowsFetched = false;
				_state = StatementState.Allocated;
				_statementType = DbStatementType.None;
			}
		}

		private void UpdateRecordsAffected()
		{
			if (ReturnRecordsAffected &&
				(StatementType == DbStatementType.Insert ||
				StatementType == DbStatementType.Delete ||
				StatementType == DbStatementType.Update ||
				StatementType == DbStatementType.StoredProcedure))
			{
				_recordsAffected = GetRecordsAffected();
			}
			else
			{
				_recordsAffected = -1;
			}
		}

		#endregion
	}
}
