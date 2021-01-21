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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed.Version10
{
	internal sealed class GdsArray : ArrayBase
	{
		const long ArrayHandle = 0;

		#region Fields

		private long _handle;
		private GdsDatabase _database;
		private GdsTransaction _transaction;

		#endregion

		#region Properties

		public override long Handle
		{
			get { return _handle; }
			set { _handle = value; }
		}

		public override IDatabase Database
		{
			get { return _database; }
			set { _database = (GdsDatabase)value; }
		}

		public override TransactionBase Transaction
		{
			get { return _transaction; }
			set { _transaction = (GdsTransaction)value; }
		}

		#endregion

		#region Constructors

		public GdsArray(ArrayDesc descriptor)
			: base(descriptor)
		{ }

		public GdsArray(IDatabase db, TransactionBase transaction, string tableName, string fieldName)
			: this(db, transaction, -1, tableName, fieldName)
		{ }

		public GdsArray(IDatabase db, TransactionBase transaction, long handle, string tableName, string fieldName)
			: base(tableName, fieldName)
		{
			if (!(db is GdsDatabase))
			{
				throw new ArgumentException($"Specified argument is not of {nameof(GdsDatabase)} type.");
			}

			if (!(transaction is GdsTransaction))
			{
				throw new ArgumentException($"Specified argument is not of {nameof(GdsTransaction)} type.");
			}

			_database = (GdsDatabase)db;
			_transaction = (GdsTransaction)transaction;
			_handle = handle;
		}

		#endregion

		#region Methods

		public override async Task<byte[]> GetSlice(int sliceLength, AsyncWrappingCommonArgs async)
		{
			try
			{
				var sdl = GenerateSDL(Descriptor);

				await _database.Xdr.Write(IscCodes.op_get_slice, async).ConfigureAwait(false);
				await _database.Xdr.Write(_transaction.Handle, async).ConfigureAwait(false);
				await _database.Xdr.Write(_handle, async).ConfigureAwait(false);
				await _database.Xdr.Write(sliceLength, async).ConfigureAwait(false);
				await _database.Xdr.WriteBuffer(sdl, async).ConfigureAwait(false);
				await _database.Xdr.Write(string.Empty, async).ConfigureAwait(false);
				await _database.Xdr.Write(0, async).ConfigureAwait(false);
				await _database.Xdr.Flush(async).ConfigureAwait(false);

				return await ReceiveSliceResponse(Descriptor, async).ConfigureAwait(false);
			}
			catch (IOException ex)
			{
				throw IscException.ForErrorCode(IscCodes.isc_network_error, ex);
			}
		}

		public override async Task PutSlice(Array sourceArray, int sliceLength, AsyncWrappingCommonArgs async)
		{
			try
			{
				var sdl = GenerateSDL(Descriptor);
				var slice = await EncodeSliceArray(sourceArray, async).ConfigureAwait(false);

				await _database.Xdr.Write(IscCodes.op_put_slice, async).ConfigureAwait(false);
				await _database.Xdr.Write(_transaction.Handle, async).ConfigureAwait(false);
				await _database.Xdr.Write(ArrayHandle, async).ConfigureAwait(false);
				await _database.Xdr.Write(sliceLength, async).ConfigureAwait(false);
				await _database.Xdr.WriteBuffer(sdl, async).ConfigureAwait(false);
				await _database.Xdr.Write(string.Empty, async).ConfigureAwait(false);
				await _database.Xdr.Write(sliceLength, async).ConfigureAwait(false);
				await _database.Xdr.WriteBytes(slice, slice.Length, async).ConfigureAwait(false);
				await _database.Xdr.Flush(async).ConfigureAwait(false);

				var response = (GenericResponse)await _database.ReadResponse(async).ConfigureAwait(false);

				_handle = response.BlobId;
			}
			catch (IOException ex)
			{
				throw IscException.ForErrorCode(IscCodes.isc_network_error, ex);
			}
		}

		#endregion

		#region Protected Methods

		protected override async Task<Array> DecodeSlice(byte[] slice, AsyncWrappingCommonArgs async)
		{
			var dbType = DbDataType.Array;
			Array sliceData = null;
			Array tempData = null;
			var systemType = GetSystemType();
			var lengths = new int[Descriptor.Dimensions];
			var lowerBounds = new int[Descriptor.Dimensions];
			var type = 0;
			var index = 0;

			for (var i = 0; i < Descriptor.Dimensions; i++)
			{
				lowerBounds[i] = Descriptor.Bounds[i].LowerBound;
				lengths[i] = Descriptor.Bounds[i].UpperBound;

				if (lowerBounds[i] == 0)
				{
					lengths[i]++;
				}
			}

			sliceData = Array.CreateInstance(systemType, lengths, lowerBounds);
			tempData = Array.CreateInstance(systemType, sliceData.Length);

			type = TypeHelper.GetSqlTypeFromBlrType(Descriptor.DataType);
			dbType = TypeHelper.GetDbDataTypeFromBlrType(Descriptor.DataType, 0, Descriptor.Scale);

			using (var ms = new MemoryStream(slice))
			{
				var xdr = new XdrReaderWriter(ms, _database.Charset);
				while (ms.Position < ms.Length)
				{
					switch (dbType)
					{
						case DbDataType.Char:
							tempData.SetValue(await xdr.ReadString(Descriptor.Length, async).ConfigureAwait(false), index);
							break;

						case DbDataType.VarChar:
							tempData.SetValue(await xdr.ReadString(async).ConfigureAwait(false), index);
							break;

						case DbDataType.SmallInt:
							tempData.SetValue(await xdr.ReadInt16(async).ConfigureAwait(false), index);
							break;

						case DbDataType.Integer:
							tempData.SetValue(await xdr.ReadInt32(async).ConfigureAwait(false), index);
							break;

						case DbDataType.BigInt:
							tempData.SetValue(await xdr.ReadInt64(async).ConfigureAwait(false), index);
							break;

						case DbDataType.Numeric:
						case DbDataType.Decimal:
							tempData.SetValue(await xdr.ReadDecimal(type, Descriptor.Scale, async).ConfigureAwait(false), index);
							break;

						case DbDataType.Float:
							tempData.SetValue(await xdr.ReadSingle(async).ConfigureAwait(false), index);
							break;

						case DbDataType.Double:
							tempData.SetValue(await xdr.ReadDouble(async).ConfigureAwait(false), index);
							break;

						case DbDataType.Date:
							tempData.SetValue(await xdr.ReadDate(async).ConfigureAwait(false), index);
							break;

						case DbDataType.Time:
							tempData.SetValue(await xdr.ReadTime(async).ConfigureAwait(false), index);
							break;

						case DbDataType.TimeStamp:
							tempData.SetValue(await xdr.ReadDateTime(async).ConfigureAwait(false), index);
							break;
					}

					index++;
				}

				if (systemType.GetTypeInfo().IsPrimitive)
				{
					// For primitive types we can use System.Buffer	to copy	generated data to destination array
					Buffer.BlockCopy(tempData, 0, sliceData, 0, Buffer.ByteLength(tempData));
				}
				else
				{
					sliceData = tempData;
				}
			}

			return sliceData;
		}

		#endregion

		#region Private Methods

		private async Task<byte[]> ReceiveSliceResponse(ArrayDesc desc, AsyncWrappingCommonArgs async)
		{
			try
			{
				var operation = await _database.ReadOperation(async).ConfigureAwait(false);
				if (operation == IscCodes.op_slice)
				{
					var isVariying = false;
					var elements = 0;
					var length = await _database.Xdr.ReadInt32(async).ConfigureAwait(false);

					length = await _database.Xdr.ReadInt32(async).ConfigureAwait(false);

					switch (desc.DataType)
					{
						case IscCodes.blr_text:
						case IscCodes.blr_text2:
						case IscCodes.blr_cstring:
						case IscCodes.blr_cstring2:
							elements = length / desc.Length;
							length += elements * ((4 - desc.Length) & 3);
							break;

						case IscCodes.blr_varying:
						case IscCodes.blr_varying2:
							elements = length / desc.Length;
							isVariying = true;
							break;

						case IscCodes.blr_short:
							length = length * desc.Length;
							break;
					}

					if (isVariying)
					{
						using (var ms = new MemoryStream())
						{
							var xdr = new XdrReaderWriter(ms);
							for (var i = 0; i < elements; i++)
							{
								var buffer = await _database.Xdr.ReadOpaque(await _database.Xdr.ReadInt32(async).ConfigureAwait(false), async).ConfigureAwait(false);
								await xdr.WriteBuffer(buffer, buffer.Length, async).ConfigureAwait(false);
							}
							await xdr.Flush(async).ConfigureAwait(false);
							return ms.ToArray();
						}
					}
					else
					{
						return await _database.Xdr.ReadOpaque(length, async).ConfigureAwait(false);
					}
				}
				else
				{
					await _database.ReadResponse(operation, async).ConfigureAwait(false);
					return null;
				}
			}
			catch (IOException ex)
			{
				throw IscException.ForErrorCode(IscCodes.isc_network_error, ex);
			}
		}

		private async Task<byte[]> EncodeSliceArray(Array sourceArray, AsyncWrappingCommonArgs async)
		{
			var dbType = DbDataType.Array;
			var charset = _database.Charset;
			var subType = (Descriptor.Scale < 0) ? 2 : 0;
			var type = 0;

			using (var ms = new MemoryStream())
			{
				var xdr = new XdrReaderWriter(ms, _database.Charset);

				type = TypeHelper.GetSqlTypeFromBlrType(Descriptor.DataType);
				dbType = TypeHelper.GetDbDataTypeFromBlrType(Descriptor.DataType, subType, Descriptor.Scale);

				foreach (var source in sourceArray)
				{
					switch (dbType)
					{
						case DbDataType.Char:
							var buffer = charset.GetBytes(source.ToString());
							await xdr.WriteOpaque(buffer, Descriptor.Length, async).ConfigureAwait(false);
							break;

						case DbDataType.VarChar:
							await xdr.Write((string)source, async).ConfigureAwait(false);
							break;

						case DbDataType.SmallInt:
							await xdr.Write((short)source, async).ConfigureAwait(false);
							break;

						case DbDataType.Integer:
							await xdr.Write((int)source, async).ConfigureAwait(false);
							break;

						case DbDataType.BigInt:
							await xdr.Write((long)source, async).ConfigureAwait(false);
							break;

						case DbDataType.Decimal:
						case DbDataType.Numeric:
							await xdr.Write((decimal)source, type, Descriptor.Scale, async).ConfigureAwait(false);
							break;

						case DbDataType.Float:
							await xdr.Write((float)source, async).ConfigureAwait(false);
							break;

						case DbDataType.Double:
							await xdr.Write((double)source, async).ConfigureAwait(false);
							break;

						case DbDataType.Date:
							await xdr.WriteDate(Convert.ToDateTime(source, CultureInfo.CurrentCulture.DateTimeFormat), async).ConfigureAwait(false);
							break;

						case DbDataType.Time:
							await xdr.WriteTime((TimeSpan)source, async).ConfigureAwait(false);
							break;

						case DbDataType.TimeStamp:
							await xdr.Write(Convert.ToDateTime(source, CultureInfo.CurrentCulture.DateTimeFormat), async).ConfigureAwait(false);
							break;

#warning New datatypes

						default:
							throw TypeHelper.InvalidDataType((int)dbType);
					}
				}

				await xdr.Flush(async).ConfigureAwait(false);
				return ms.ToArray();
			}
		}

		private byte[] GenerateSDL(ArrayDesc desc)
		{
			int n;
			int from;
			int to;
			int increment;
			int dimensions;
			ArrayBound tail;
			BinaryWriter sdl;

			dimensions = desc.Dimensions;

			if (dimensions > 16)
			{
				throw IscException.ForErrorCode(IscCodes.isc_invalid_dimension);
			}

			sdl = new BinaryWriter(new MemoryStream());
			Stuff(
				sdl, 4, IscCodes.isc_sdl_version1,
				IscCodes.isc_sdl_struct, 1, desc.DataType);

			switch (desc.DataType)
			{
				case IscCodes.blr_short:
				case IscCodes.blr_long:
				case IscCodes.blr_int64:
				case IscCodes.blr_quad:
					StuffSdl(sdl, (byte)desc.Scale);
					break;

				case IscCodes.blr_text:
				case IscCodes.blr_cstring:
				case IscCodes.blr_varying:
					StuffWord(sdl, desc.Length);
					break;

				default:
					break;
			}

			StuffString(sdl, IscCodes.isc_sdl_relation, desc.RelationName);
			StuffString(sdl, IscCodes.isc_sdl_field, desc.FieldName);

			if ((desc.Flags & IscCodes.ARRAY_DESC_COLUMN_MAJOR) == IscCodes.ARRAY_DESC_COLUMN_MAJOR)
			{
				from = dimensions - 1;
				to = -1;
				increment = -1;
			}
			else
			{
				from = 0;
				to = dimensions;
				increment = 1;
			}

			for (n = from; n != to; n += increment)
			{
				tail = desc.Bounds[n];
				if (tail.LowerBound == 1)
				{
					Stuff(sdl, 2, IscCodes.isc_sdl_do1, n);
				}
				else
				{
					Stuff(sdl, 2, IscCodes.isc_sdl_do2, n);

					StuffLiteral(sdl, tail.LowerBound);
				}

				StuffLiteral(sdl, tail.UpperBound);
			}

			Stuff(sdl, 5, IscCodes.isc_sdl_element, 1, IscCodes.isc_sdl_scalar, 0, dimensions);

			for (n = 0; n < dimensions; n++)
			{
				Stuff(sdl, 2, IscCodes.isc_sdl_variable, n);
			}

			StuffSdl(sdl, IscCodes.isc_sdl_eoc);

			return ((MemoryStream)sdl.BaseStream).ToArray();
		}

		private void Stuff(BinaryWriter sdl, short count, params object[] args)
		{
			for (var i = 0; i < count; i++)
			{
				sdl.Write(Convert.ToByte(args[i], CultureInfo.InvariantCulture));
			}
		}

		private void Stuff(BinaryWriter sdl, byte[] args)
		{
			sdl.Write(args);
		}

		private void StuffSdl(BinaryWriter sdl, byte sdl_byte)
		{
			Stuff(sdl, 1, sdl_byte);
		}

		private void StuffWord(BinaryWriter sdl, short word)
		{
			Stuff(sdl, BitConverter.GetBytes(word));
		}

		private void StuffLong(BinaryWriter sdl, int word)
		{
			Stuff(sdl, BitConverter.GetBytes(word));
		}

		private void StuffLiteral(BinaryWriter sdl, int literal)
		{
			if (literal >= -128 && literal <= 127)
			{
				Stuff(sdl, 2, IscCodes.isc_sdl_tiny_integer, literal);

				return;
			}

			if (literal >= -32768 && literal <= 32767)
			{
				StuffSdl(sdl, IscCodes.isc_sdl_short_integer);
				StuffWord(sdl, (short)literal);

				return;
			}

			StuffSdl(sdl, IscCodes.isc_sdl_long_integer);
			StuffLong(sdl, literal);
		}

		private void StuffString(BinaryWriter sdl, int constant, string value)
		{
			StuffSdl(sdl, (byte)constant);
			StuffSdl(sdl, (byte)value.Length);

			for (var i = 0; i < value.Length; i++)
			{
				StuffSdl(sdl, (byte)value[i]);
			}
		}

		#endregion
	}
}
