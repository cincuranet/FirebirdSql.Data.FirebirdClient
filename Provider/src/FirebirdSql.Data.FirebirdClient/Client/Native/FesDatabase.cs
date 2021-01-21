﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/blob/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$Authors = Carlos Guzman Alvarez, Jiri Cincura (jiri@cincura.net)

using System;
using System.Threading;
using System.Text;

using FirebirdSql.Data.Common;
using FirebirdSql.Data.Client.Native.Handle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirebirdSql.Data.Client.Native
{
	internal sealed class FesDatabase : IDatabase
	{
		#region Callbacks

		public Action<IscException> WarningMessage
		{
			get { return _warningMessage; }
			set { _warningMessage = value; }
		}

		#endregion

		#region Fields

		private Action<IscException> _warningMessage;

		private DatabaseHandle _handle;
		private int _transactionCount;
		private string _serverVersion;
		private Charset _charset;
		private short _packetSize;
		private short _dialect;
		private IntPtr[] _statusVector;

		private IFbClient _fbClient;

		#endregion

		#region Properties

		public int Handle
		{
			get { return _handle.DangerousGetHandle().AsInt(); }
		}

		public DatabaseHandle HandlePtr
		{
			get { return _handle; }
		}

		public int TransactionCount
		{
			get { return _transactionCount; }
			set { _transactionCount = value; }
		}

		public string ServerVersion
		{
			get { return _serverVersion; }
		}

		public Charset Charset
		{
			get { return _charset; }
			set { _charset = value; }
		}

		public short PacketSize
		{
			get { return _packetSize; }
			set { _packetSize = value; }
		}

		public short Dialect
		{
			get { return _dialect; }
			set { _dialect = value; }
		}

		public bool HasRemoteEventSupport
		{
			get { return false; }
		}

		public IFbClient FbClient
		{
			get { return _fbClient; }
		}

		public bool ConnectionBroken
		{
			get { return false; }
		}

		#endregion

		#region Constructors

		public FesDatabase(string dllName, Charset charset)
		{
			_fbClient = FbClientFactory.Create(dllName);
			_handle = new DatabaseHandle();
			_charset = charset ?? Charset.DefaultCharset;
			_dialect = 3;
			_packetSize = 8192;
			_statusVector = new IntPtr[IscCodes.ISC_STATUS_LENGTH];
		}

		#endregion

		#region Database Methods

		public Task CreateDatabase(DatabaseParameterBufferBase dpb, string dataSource, int port, string database, byte[] cryptKey, AsyncWrappingCommonArgs async)
		{
			CheckCryptKeyForSupport(cryptKey);

			var databaseBuffer = Encoding.Default.GetBytes(database);

			ClearStatusVector();

			_fbClient.isc_create_database(
				_statusVector,
				(short)databaseBuffer.Length,
				databaseBuffer,
				ref _handle,
				dpb.Length,
				dpb.ToArray(),
				0);

			ProcessStatusVector(_statusVector);

			return Task.CompletedTask;
		}

		public Task CreateDatabaseWithTrustedAuth(DatabaseParameterBufferBase dpb, string dataSource, int port, string database, byte[] cryptKey, AsyncWrappingCommonArgs async)
		{
			throw new NotSupportedException("Trusted Auth isn't supported on Firebird Embedded.");
		}

		public Task DropDatabase(AsyncWrappingCommonArgs async)
		{
			ClearStatusVector();

			_fbClient.isc_drop_database(_statusVector, ref _handle);

			ProcessStatusVector(_statusVector);

			_handle.Dispose();

			return Task.CompletedTask;
		}

		#endregion

		#region Remote Events Methods

		public Task CloseEventManager(AsyncWrappingCommonArgs async)
		{
			throw new NotSupportedException();
		}

		public Task QueueEvents(RemoteEvent events, AsyncWrappingCommonArgs async)
		{
			throw new NotSupportedException();
		}

		public Task CancelEvents(RemoteEvent events, AsyncWrappingCommonArgs async)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Methods

		public async Task Attach(DatabaseParameterBufferBase dpb, string dataSource, int port, string database, byte[] cryptKey, AsyncWrappingCommonArgs async)
		{
			CheckCryptKeyForSupport(cryptKey);

			var databaseBuffer = Encoding.Default.GetBytes(database);

			ClearStatusVector();

			_fbClient.isc_attach_database(
				_statusVector,
				(short)databaseBuffer.Length,
				databaseBuffer,
				ref _handle,
				dpb.Length,
				dpb.ToArray());

			ProcessStatusVector(_statusVector);

			_serverVersion = await GetServerVersion(async).ConfigureAwait(false);
		}

		public Task AttachWithTrustedAuth(DatabaseParameterBufferBase dpb, string dataSource, int port, string database, byte[] cryptKey, AsyncWrappingCommonArgs async)
		{
			throw new NotSupportedException("Trusted Auth isn't supported on Firebird Embedded.");
		}

		public Task Detach(AsyncWrappingCommonArgs async)
		{
			if (TransactionCount > 0)
			{
				throw IscException.ForErrorCodeIntParam(IscCodes.isc_open_trans, TransactionCount);
			}

			if (!_handle.IsInvalid)
			{
				ClearStatusVector();

				_fbClient.isc_detach_database(_statusVector, ref _handle);

				ProcessStatusVector(_statusVector);

				_handle.Dispose();
			}

			_warningMessage = null;
			_charset = null;
			_serverVersion = null;
			_statusVector = null;
			_transactionCount = 0;
			_dialect = 0;
			_packetSize = 0;

			return Task.CompletedTask;
		}

		#endregion

		#region Transaction Methods

		public async Task<TransactionBase> BeginTransaction(TransactionParameterBuffer tpb, AsyncWrappingCommonArgs async)
		{
			var transaction = new FesTransaction(this);
			await transaction.BeginTransaction(tpb, async).ConfigureAwait(false);
			return transaction;
		}

		#endregion

		#region Cancel Methods

		public Task CancelOperation(int kind, AsyncWrappingCommonArgs async)
		{
			var localStatusVector = new IntPtr[IscCodes.ISC_STATUS_LENGTH];

			_fbClient.fb_cancel_operation(localStatusVector, ref _handle, kind);

			ProcessStatusVector(localStatusVector);

			return Task.CompletedTask;
		}

		#endregion

		#region Statement Creation Methods

		public StatementBase CreateStatement()
		{
			return new FesStatement(this);
		}

		public StatementBase CreateStatement(TransactionBase transaction)
		{
			return new FesStatement(this, transaction as FesTransaction);
		}

		#endregion

		#region DPB

		public DatabaseParameterBufferBase CreateDatabaseParameterBuffer()
		{
			return new DatabaseParameterBuffer1();
		}

		#endregion

		#region Database Information Methods

		public async Task<string> GetServerVersion(AsyncWrappingCommonArgs async)
		{
#warning This method is duplicate of what is in GdsDatabase
			var items = new byte[]
			{
				IscCodes.isc_info_firebird_version,
				IscCodes.isc_info_end
			};

			return (await GetDatabaseInfo(items, IscCodes.BUFFER_SIZE_128, async).ConfigureAwait(false))[0].ToString();
		}

		public Task<List<object>> GetDatabaseInfo(byte[] items, AsyncWrappingCommonArgs async)
		{
			return GetDatabaseInfo(items, IscCodes.DEFAULT_MAX_BUFFER_SIZE, async);
		}

		public Task<List<object>> GetDatabaseInfo(byte[] items, int bufferLength, AsyncWrappingCommonArgs async)
		{
			var buffer = new byte[bufferLength];

			DatabaseInfo(items, buffer, buffer.Length);

			return Task.FromResult(IscHelper.ParseDatabaseInfo(buffer));
		}

		#endregion

		#region Internal Methods

		internal void ProcessStatusVector(IntPtr[] statusVector)
		{
			var ex = FesConnection.ParseStatusVector(statusVector, _charset);

			if (ex != null)
			{
				if (ex.IsWarning)
				{
					_warningMessage?.Invoke(ex);
				}
				else
				{
					throw ex;
				}
			}
		}

		#endregion

		#region Private Methods

		private void ClearStatusVector()
		{
			Array.Clear(_statusVector, 0, _statusVector.Length);
		}

		private void DatabaseInfo(byte[] items, byte[] buffer, int bufferLength)
		{
			ClearStatusVector();

			_fbClient.isc_database_info(
				_statusVector,
				ref _handle,
				(short)items.Length,
				items,
				(short)bufferLength,
				buffer);

			ProcessStatusVector(_statusVector);
		}

		#endregion

		#region Internal Static Methods

		internal static void CheckCryptKeyForSupport(byte[] cryptKey)
		{
			// ICryptKeyCallbackImpl would have to be passed from C# for 'cryptKey' passing
			if (cryptKey?.Length > 0)
				throw new NotSupportedException("Passing Encryption Key isn't, yet, supported on Firebird Embedded.");
		}

		#endregion
	}
}
