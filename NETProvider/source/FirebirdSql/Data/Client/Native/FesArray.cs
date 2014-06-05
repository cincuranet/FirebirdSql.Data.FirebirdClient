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
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

using FirebirdSql.Data.Common;
using FirebirdSql.Data.Client.Common;

namespace FirebirdSql.Data.Client.Native
{
	internal sealed class FesArray : ArrayBase
	{
		#region � Fields �

		private long handle;
		private FesDatabase db;
		private FesTransaction transaction;
		private IntPtr[] statusVector;

		#endregion

		#region � Properties �

		public override long Handle
		{
			get { return this.handle; }
			set { this.handle = value; }
		}

		public override IDatabase DB
		{
			get { return this.db; }
			set { this.db = (FesDatabase)value; }
		}

		public override ITransaction Transaction
		{
			get { return this.transaction; }
			set { this.transaction = (FesTransaction)value; }
		}

		#endregion

		#region � Constructors �

		public FesArray(ArrayDesc descriptor)
			: base(descriptor)
		{
			this.statusVector = new IntPtr[IscCodes.ISC_STATUS_LENGTH];
		}

		public FesArray(
			IDatabase db,
			ITransaction transaction,
			string tableName,
			string fieldName)
			: this(db, transaction, -1, tableName, fieldName)
		{
		}

		public FesArray(
			IDatabase db,
			ITransaction transaction,
			long handle,
			string tableName,
			string fieldName)
			: base(tableName, fieldName)
		{
			if (!(db is FesDatabase))
			{
				throw new ArgumentException("Specified argument is not of GdsDatabase type.");
			}
			if (!(transaction is FesTransaction))
			{
				throw new ArgumentException("Specified argument is not of GdsTransaction type.");
			}
			this.db = (FesDatabase)db;
			this.transaction = (FesTransaction)transaction;
			this.handle = handle;
			this.statusVector = new IntPtr[IscCodes.ISC_STATUS_LENGTH];

			this.LookupBounds();
		}

		#endregion

		#region � Methods �

		public override byte[] GetSlice(int sliceLength)
		{
			// Clear the status vector
			this.ClearStatusVector();

			int dbHandle = this.db.Handle;
			int trHandle = this.transaction.Handle;

			ArrayDescMarshaler marshaler = ArrayDescMarshaler.Instance;

			IntPtr arrayDesc = marshaler.MarshalManagedToNative(this.Descriptor);

			byte[] buffer = new byte[sliceLength];

			db.FbClient.isc_array_get_slice(
				this.statusVector,
				ref	dbHandle,
				ref	trHandle,
				ref	this.handle,
				arrayDesc,
				buffer,
				ref	sliceLength);

			// Free	memory
			marshaler.CleanUpNativeData(ref	arrayDesc);

			FesConnection.ParseStatusVector(this.statusVector, this.db.Charset);

			return buffer;
		}

		public override void PutSlice(System.Array sourceArray, int sliceLength)
		{
			// Clear the status vector
			this.ClearStatusVector();

			int dbHandle = this.db.Handle;
			int trHandle = this.transaction.Handle;

			ArrayDescMarshaler marshaler = ArrayDescMarshaler.Instance;

			IntPtr arrayDesc = marshaler.MarshalManagedToNative(this.Descriptor);

			// Obtain the System of	type of	Array elements and
			// Fill	buffer
			Type systemType = this.GetSystemType();

			byte[] buffer = new byte[sliceLength];
			if (systemType.IsPrimitive)
			{
				Buffer.BlockCopy(sourceArray, 0, buffer, 0, buffer.Length);
			}
			else
			{
				buffer = this.EncodeSlice(this.Descriptor, sourceArray, sliceLength);
			}

			db.FbClient.isc_array_put_slice(
				this.statusVector,
				ref	dbHandle,
				ref	trHandle,
				ref	this.handle,
				arrayDesc,
				buffer,
				ref	sliceLength);

			// Free	memory
			marshaler.CleanUpNativeData(ref	arrayDesc);

			FesConnection.ParseStatusVector(this.statusVector, this.db.Charset);
		}

		#endregion

		#region � Protected Methods �

		protected override System.Array DecodeSlice(byte[] slice)
		{
			Array sliceData = null;
			int slicePosition = 0;
			int type = 0;
			DbDataType dbType = DbDataType.Array;
			Type systemType = this.GetSystemType();
			Charset charset = this.db.Charset;
			int[] lengths = new int[this.Descriptor.Dimensions];
			int[] lowerBounds = new int[this.Descriptor.Dimensions];

			// Get upper and lower bounds of each dimension
			for (int i = 0; i < this.Descriptor.Dimensions; i++)
			{
				lowerBounds[i] = this.Descriptor.Bounds[i].LowerBound;
				lengths[i] = this.Descriptor.Bounds[i].UpperBound;

				if (lowerBounds[i] == 0)
				{
					lengths[i]++;
				}
			}

			// Create slice	arrays
			sliceData = Array.CreateInstance(systemType, lengths, lowerBounds);

			Array tempData = Array.CreateInstance(systemType, sliceData.Length);

			// Infer data types
			type = TypeHelper.GetFbType(this.Descriptor.DataType);
			dbType = TypeHelper.GetDbDataType(this.Descriptor.DataType, 0, this.Descriptor.Scale);

			int itemLength = this.Descriptor.Length;

			for (int i = 0; i < tempData.Length; i++)
			{
				if (slicePosition >= slice.Length)
				{
					break;
				}

				switch (dbType)
				{
					case DbDataType.Char:
						tempData.SetValue(charset.GetString(slice, slicePosition, itemLength), i);
						break;

					case DbDataType.VarChar:
						{
							int index = slicePosition;
							int count = 0;
							while (slice[index++] != 0)
							{
								count++;
							}
							tempData.SetValue(charset.GetString(slice, slicePosition, count), i);

							slicePosition += 2;
						}
						break;

					case DbDataType.SmallInt:
						tempData.SetValue(BitConverter.ToInt16(slice, slicePosition), i);
						break;

					case DbDataType.Integer:
						tempData.SetValue(BitConverter.ToInt32(slice, slicePosition), i);
						break;

					case DbDataType.BigInt:
						tempData.SetValue(BitConverter.ToInt64(slice, slicePosition), i);
						break;

					case DbDataType.Decimal:
					case DbDataType.Numeric:
						{
							object evalue = null;

							switch (type)
							{
								case IscCodes.SQL_SHORT:
									evalue = BitConverter.ToInt16(slice, slicePosition);
									break;

								case IscCodes.SQL_LONG:
									evalue = BitConverter.ToInt32(slice, slicePosition);
									break;

								case IscCodes.SQL_QUAD:
								case IscCodes.SQL_INT64:
									evalue = BitConverter.ToInt64(slice, slicePosition);
									break;
							}

							decimal dvalue = TypeDecoder.DecodeDecimal(evalue, this.Descriptor.Scale, type);

							tempData.SetValue(dvalue, i);
						}
						break;

					case DbDataType.Double:
						tempData.SetValue(BitConverter.ToDouble(slice, slicePosition), i);
						break;

					case DbDataType.Float:
						tempData.SetValue(BitConverter.ToSingle(slice, slicePosition), i);
						break;

					case DbDataType.Date:
						{
							int idate = BitConverter.ToInt32(slice, slicePosition);

							DateTime date = TypeDecoder.DecodeDate(idate);

							tempData.SetValue(date, i);
						}
						break;

					case DbDataType.Time:
						{
							int itime = BitConverter.ToInt32(slice, slicePosition);

							TimeSpan time = TypeDecoder.DecodeTime(itime);

							tempData.SetValue(time, i);
						}
						break;

					case DbDataType.TimeStamp:
						{
							int idate = BitConverter.ToInt32(slice, slicePosition);
							int itime = BitConverter.ToInt32(slice, slicePosition + 4);

							DateTime date = TypeDecoder.DecodeDate(idate);
							TimeSpan time = TypeDecoder.DecodeTime(itime);

							DateTime timestamp = new System.DateTime(
								date.Year, date.Month, date.Day,
								time.Hours, time.Minutes, time.Seconds, time.Milliseconds);

							tempData.SetValue(timestamp, i);
						}
						break;
				}

				slicePosition += itemLength;
			}

			if (systemType.IsPrimitive)
			{
				// For primitive types we can use System.Buffer	to copy	generated data to destination array
				Buffer.BlockCopy(tempData, 0, sliceData, 0, Buffer.ByteLength(tempData));
			}
			else
			{
				sliceData = tempData;
			}

			return sliceData;
		}

		#endregion

		#region � Private Metods �

		private void ClearStatusVector()
		{
			Array.Clear(this.statusVector, 0, this.statusVector.Length);
		}

		private byte[] EncodeSlice(ArrayDesc desc, Array sourceArray, int length)
		{
			BinaryWriter writer = new BinaryWriter(new MemoryStream());
			Charset charset = this.db.Charset;
			DbDataType dbType = DbDataType.Array;
			int subType = (this.Descriptor.Scale < 0) ? 2 : 0;
			int type = 0;

			// Infer data types
			type = TypeHelper.GetFbType(this.Descriptor.DataType);
			dbType = TypeHelper.GetDbDataType(this.Descriptor.DataType, subType, this.Descriptor.Scale);

			foreach (object source in sourceArray)
			{
				switch (dbType)
				{
					case DbDataType.Char:
						{
							string value = source != null ? (string)source : string.Empty;
							byte[] buffer = charset.GetBytes(value);

							writer.Write(buffer);

							if (desc.Length > buffer.Length)
							{
								for (int j = buffer.Length; j < desc.Length; j++)
								{
									writer.Write((byte)32);
								}
							}
						}
						break;

					case DbDataType.VarChar:
						{
							string value = source != null ? (string)source : string.Empty;

							byte[] buffer = charset.GetBytes(value);
							writer.Write(buffer);

							if (desc.Length > buffer.Length)
							{
								for (int j = buffer.Length; j < desc.Length; j++)
								{
									writer.Write((byte)0);
								}
							}
							writer.Write((short)0);
						}
						break;

					case DbDataType.SmallInt:
						writer.Write((short)source);
						break;

					case DbDataType.Integer:
						writer.Write((int)source);
						break;

					case DbDataType.BigInt:
						writer.Write((long)source);
						break;

					case DbDataType.Float:
						writer.Write((float)source);
						break;

					case DbDataType.Double:
						writer.Write((double)source);
						break;

					case DbDataType.Numeric:
					case DbDataType.Decimal:
						{
							object numeric = TypeEncoder.EncodeDecimal((decimal)source, desc.Scale, type);

							switch (type)
							{
								case IscCodes.SQL_SHORT:
									writer.Write((short)numeric);
									break;

								case IscCodes.SQL_LONG:
									writer.Write((int)numeric);
									break;

								case IscCodes.SQL_QUAD:
								case IscCodes.SQL_INT64:
									writer.Write((long)numeric);
									break;
							}
						}
						break;

					case DbDataType.Date:
						writer.Write(TypeEncoder.EncodeDate(Convert.ToDateTime(source, CultureInfo.CurrentCulture.DateTimeFormat)));
						break;

					case DbDataType.Time:
						writer.Write(TypeEncoder.EncodeTime((TimeSpan)source));
						break;

					case DbDataType.TimeStamp:
						writer.Write(TypeEncoder.EncodeDate(Convert.ToDateTime(source, CultureInfo.CurrentCulture.DateTimeFormat)));
						writer.Write(TypeEncoder.EncodeTime(((DateTime)source).TimeOfDay));
						break;

					default:
						throw new NotSupportedException("Unknown data type");
				}
			}

			return ((MemoryStream)writer.BaseStream).ToArray();
		}

		#endregion
	}
}
