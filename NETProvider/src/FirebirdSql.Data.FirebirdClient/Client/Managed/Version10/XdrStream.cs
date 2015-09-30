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
 *	Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *	All Rights Reserved.
 *
 *  Contributors:
 *    Jiri Cincura (jiri@cincura.net)
 */

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Globalization;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed.Version10
{
	internal class XdrStream : Stream
	{
		#region Static Fields

		private static byte[] fill;
		private static byte[] pad;

		#endregion

		#region Static Properties

		internal static byte[] Fill
		{
			get
			{
				if (fill == null)
				{
					fill = new byte[32767];
					for (int i = 0; i < fill.Length; i++)
					{
						fill[i] = 32;
					}
				}

				return fill;
			}
		}

		private static byte[] Pad
		{
			get { return pad ?? (pad = new byte[] { 0, 0, 0, 0 }); }
		}

		#endregion

		#region Fields

		private Stream _innerStream;
		private Charset _charset;
		private bool _ownsStream;
		private int _operation;

		#endregion

		#region Stream Properties

		public override bool CanWrite
		{
			get { return _innerStream.CanWrite; }
		}

		public override bool CanRead
		{
			get { return _innerStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _innerStream.CanSeek; }
		}

		public override long Position
		{
			get { return _innerStream.Position; }
			set { _innerStream.Position = value; }
		}

		public override long Length
		{
			get { return _innerStream.Length; }
		}

		#endregion

		#region Constructors

		public XdrStream()
			: this(Charset.DefaultCharset)
		{ }

		public XdrStream(Charset charset)
			: this(new MemoryStream(), charset, true)
		{ }

		public XdrStream(byte[] buffer, Charset charset)
			: this(new MemoryStream(buffer), charset, true)
		{ }

		public XdrStream(Stream innerStream, Charset charset, bool ownsStream)
			: base()
		{
			_innerStream = innerStream;
			_charset = charset;
			_ownsStream = ownsStream;
			ResetOperation();
		}

		#endregion

		#region Stream methods

		public override void Close()
		{
			try
			{
				if (_ownsStream)
				{
					_innerStream?.Close();
				}
			}
			catch
			{ }
			finally
			{
			_innerStream = null;
			_charset = null;
		}

		public override void Flush()
		{
			CheckDisposed();

			_innerStream.Flush();
		}

		public override void SetLength(long length)
		{
			CheckDisposed();

			_innerStream.SetLength(length);
		}

		public override long Seek(long offset, System.IO.SeekOrigin loc)
		{
			CheckDisposed();

			return _innerStream.Seek(offset, loc);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckDisposed();

			if (!CanRead)
				throw new InvalidOperationException("Read operations are not allowed by this stream");

			return _innerStream.Read(buffer, offset, count);
		}

		public override void WriteByte(byte value)
		{
			CheckDisposed();

			_innerStream.WriteByte(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			CheckDisposed();

			if (!CanWrite)
				throw new InvalidOperationException("Write operations are not allowed by this stream");

			_innerStream.Write(buffer, offset, count);
		}

		public byte[] ToArray()
		{
			CheckDisposed();

			var memoryStream = _innerStream as MemoryStream;
			if (memoryStream == null)
				throw new InvalidOperationException();
			return memoryStream.ToArray();
		}

		#endregion

		#region Operation Identification Methods

		public virtual int ReadOperation()
		{
			var op = ValidOperationAvailable ? _operation : ReadNextOperation();
			ResetOperation();
			return op;
		}

		public virtual int ReadNextOperation()
		{
			do
			{
				/* loop	as long	as we are receiving	dummy packets, just
				 * throwing	them away--note	that if	we are a server	we won't
				 * be receiving	them, but it is	better to check	for	them at
				 * this	level rather than try to catch them	in all places where
				 * this	routine	is called
				 */
				_operation = ReadInt32();
			} while (_operation == IscCodes.op_dummy);

			return _operation;
		}

		#endregion

		#region XDR Read Methods

		public byte[] ReadBytes(int count)
		{
			var buffer = new byte[count];
			if (count > 0)
			{
				var toRead = count;
				var currentlyRead = -1;
				while (toRead > 0 && currentlyRead != 0)
				{
					toRead -= (currentlyRead = Read(buffer, count - toRead, toRead));
				}
				if (toRead == count)
				{
					throw new IOException();
				}
			}
			return buffer;
		}

		public byte[] ReadOpaque(int length)
		{
			var buffer = ReadBytes(length);
			var padLength = ((4 - length) & 3);
			if (padLength > 0)
			{
				Read(Pad, 0, padLength);
			}
			return buffer;
		}

		public byte[] ReadBuffer()
		{
			return ReadOpaque((ushort)ReadInt32());
		}

		public string ReadString()
		{
			return ReadString(_charset);
		}

		public string ReadString(int length)
		{
			return ReadString(_charset, length);
		}

		public string ReadString(Charset charset)
		{
			return ReadString(charset, ReadInt32());
		}

		public string ReadString(Charset charset, int length)
		{
			var buffer = ReadOpaque(length);
			return charset.GetString(buffer, 0, buffer.Length);
		}

		public short ReadInt16()
		{
			return Convert.ToInt16(ReadInt32());
		}

		public int ReadInt32()
		{
			return IPAddress.HostToNetworkOrder(BitConverter.ToInt32(ReadBytes(4), 0));
		}

		public long ReadInt64()
		{
			return IPAddress.HostToNetworkOrder(BitConverter.ToInt64(ReadBytes(8), 0));
		}

		public Guid ReadGuid(int length)
		{
			return new Guid(ReadOpaque(length));
		}

		public float ReadSingle()
		{
			return BitConverter.ToSingle(BitConverter.GetBytes(ReadInt32()), 0);
		}

		public double ReadDouble()
		{
			return BitConverter.ToDouble(BitConverter.GetBytes(ReadInt64()), 0);
		}

		public DateTime ReadDateTime()
		{
			DateTime date = ReadDate();
			TimeSpan time = ReadTime();
			return new DateTime(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds, time.Milliseconds);
		}

		public DateTime ReadDate()
		{
			return TypeDecoder.DecodeDate(ReadInt32());
		}

		public TimeSpan ReadTime()
		{
			return TypeDecoder.DecodeTime(ReadInt32());
		}

		public decimal ReadDecimal(int type, int scale)
		{
			var value = 0m;
			switch (type & ~1)
			{
				case IscCodes.SQL_SHORT:
					value = TypeDecoder.DecodeDecimal(ReadInt16(), scale, type);
					break;

				case IscCodes.SQL_LONG:
					value = TypeDecoder.DecodeDecimal(ReadInt32(), scale, type);
					break;

				case IscCodes.SQL_QUAD:
				case IscCodes.SQL_INT64:
					value = TypeDecoder.DecodeDecimal(ReadInt64(), scale, type);
					break;

				case IscCodes.SQL_DOUBLE:
				case IscCodes.SQL_D_FLOAT:
					value = Convert.ToDecimal(ReadDouble());
					break;
			}
			return value;
		}

		public object ReadValue(DbField field)
		{
			object fieldValue = null;
			Charset innerCharset = _charset.Name != "NONE" ? _charset : field.Charset;

			switch (field.DbDataType)
			{
				case DbDataType.Char:
					if (field.Charset.IsOctetsCharset)
					{
						fieldValue = ReadOpaque(field.Length);
					}
					else
					{
						string s = ReadString(innerCharset, field.Length);

						if ((field.Length % field.Charset.BytesPerCharacter) == 0 &&
							s.Length > field.CharCount)
						{
							fieldValue = s.Substring(0, field.CharCount);
						}
						else
						{
							fieldValue = s;
						}
					}
					break;

				case DbDataType.VarChar:
					if (field.Charset.IsOctetsCharset)
					{
						fieldValue = ReadBuffer();
					}
					else
					{
						fieldValue = ReadString(innerCharset);
					}
					break;

				case DbDataType.SmallInt:
					fieldValue = ReadInt16();
					break;

				case DbDataType.Integer:
					fieldValue = ReadInt32();
					break;

				case DbDataType.Array:
				case DbDataType.Binary:
				case DbDataType.Text:
				case DbDataType.BigInt:
					fieldValue = ReadInt64();
					break;

				case DbDataType.Decimal:
				case DbDataType.Numeric:
					fieldValue = ReadDecimal(field.DataType, field.NumericScale);
					break;

				case DbDataType.Float:
					fieldValue = ReadSingle();
					break;

				case DbDataType.Guid:
					fieldValue = ReadGuid(field.Length);
					break;

				case DbDataType.Double:
					fieldValue = ReadDouble();
					break;

				case DbDataType.Date:
					fieldValue = ReadDate();
					break;

				case DbDataType.Time:
					fieldValue = ReadTime();
					break;

				case DbDataType.TimeStamp:
					fieldValue = ReadDateTime();
					break;
			}

			int sqlInd = ReadInt32();

			if (sqlInd == 0)
			{
				return fieldValue;
			}
			else if (sqlInd == -1)
			{
				return null;
			}
			else
			{
				throw IscException.ForStrParam($"Invalid {nameof(sqlInd)} value: {sqlInd}.");
			}
		}

		#endregion

		#region XDR Write Methods

		public void WriteOpaque(byte[] buffer)
		{
			WriteOpaque(buffer, buffer.Length);
		}

		public void WriteOpaque(byte[] buffer, int length)
		{
			if (buffer != null && length > 0)
			{
				Write(buffer, 0, buffer.Length);
				Write(Fill, 0, length - buffer.Length);
				Write(Pad, 0, ((4 - length) & 3));
			}
		}

		public void WriteBuffer(byte[] buffer)
		{
			WriteBuffer(buffer, buffer == null ? 0 : buffer.Length);
		}

		public void WriteBuffer(byte[] buffer, int length)
		{
			Write(length);
			if (buffer != null && length > 0)
			{
				Write(buffer, 0, length);
				Write(Pad, 0, ((4 - length) & 3));
			}
		}

		public void WriteBlobBuffer(byte[] buffer)
		{
			var length = buffer.Length; // 2 for short for buffer length
			if (length > short.MaxValue)
				throw new IOException();
			Write(length + 2);
			Write(length + 2);  //bizarre but true! three copies of the length
			WriteByte((byte)((length >> 0) & 0xff));
			WriteByte((byte)((length >> 8) & 0xff));
			Write(buffer, 0, length);
			Write(Pad, 0, ((4 - length + 2) & 3));
		}

		public void WriteTyped(int type, byte[] buffer)
		{
			int length;
			if (buffer == null)
			{
				Write(1);
				WriteByte((byte)type);
				length = 1;
			}
			else
			{
				length = buffer.Length + 1;
				Write(length);
				WriteByte((byte)type);
				Write(buffer, 0, buffer.Length);
			}
			Write(Pad, 0, ((4 - length) & 3));
		}

		public void Write(string value)
		{
			byte[] buffer = _charset.GetBytes(value);
			WriteBuffer(buffer, buffer.Length);
		}

		public void Write(short value)
		{
			Write((int)value);
		}

		public void Write(int value)
		{
			Write(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value)), 0, 4);
		}

		public void Write(long value)
		{
			Write(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value)), 0, 8);
		}

		public void Write(float value)
		{
			var buffer = BitConverter.GetBytes(value);
			Write(BitConverter.ToInt32(buffer, 0));
		}

		public void Write(double value)
		{
			var buffer = BitConverter.GetBytes(value);
			Write(BitConverter.ToInt64(buffer, 0));
		}

		public void Write(decimal value, int type, int scale)
		{
			object numeric = TypeEncoder.EncodeDecimal(value, scale, type);
			switch (type & ~1)
			{
				case IscCodes.SQL_SHORT:
					Write((short)numeric);
					break;

				case IscCodes.SQL_LONG:
					Write((int)numeric);
					break;

				case IscCodes.SQL_QUAD:
				case IscCodes.SQL_INT64:
					Write((long)numeric);
					break;

				case IscCodes.SQL_DOUBLE:
				case IscCodes.SQL_D_FLOAT:
					Write((double)value);
					break;
			}
		}

		public void Write(bool value)
		{
			Write((short)(value ? 1 : 0));
		}

		public void Write(DateTime value)
		{
			WriteDate(value);
			WriteTime(TypeHelper.DateTimeToTimeSpan(value));
		}

		public void WriteDate(DateTime value)
		{
			Write(TypeEncoder.EncodeDate(Convert.ToDateTime(value)));
		}

		public void WriteTime(TimeSpan value)
		{
			Write(TypeEncoder.EncodeTime(value));
		}

		public void Write(Descriptor descriptor)
		{
			for (var i = 0; i < descriptor.Count; i++)
			{
				Write(descriptor[i]);
			}
		}

		public void Write(DbField param)
		{
			try
			{
				if (param.DbDataType != DbDataType.Null)
				{
					param.FixNull();

					switch (param.DbDataType)
					{
						case DbDataType.Char:
							if (param.Charset.IsOctetsCharset)
							{
								WriteOpaque(param.DbValue.GetBinary(), param.Length);
							}
							else
							{
								string svalue = param.DbValue.GetString();

								if ((param.Length % param.Charset.BytesPerCharacter) == 0 &&
									svalue.Length > param.CharCount)
								{
									throw IscException.ForErrorCodes(new[] { IscCodes.isc_arith_except, IscCodes.isc_string_truncation });
								}

								WriteOpaque(param.Charset.GetBytes(svalue), param.Length);
							}
							break;

						case DbDataType.VarChar:
							if (param.Charset.IsOctetsCharset)
							{
								WriteOpaque(param.DbValue.GetBinary(), param.Length);
							}
							else
							{
								string svalue = param.DbValue.GetString();

								if ((param.Length % param.Charset.BytesPerCharacter) == 0 &&
									svalue.Length > param.CharCount)
								{
									throw IscException.ForErrorCodes(new[] { IscCodes.isc_arith_except, IscCodes.isc_string_truncation });
								}

								byte[] data = param.Charset.GetBytes(svalue);

								WriteBuffer(data, data.Length);
							}
							break;

						case DbDataType.SmallInt:
							Write(param.DbValue.GetInt16());
							break;

						case DbDataType.Integer:
							Write(param.DbValue.GetInt32());
							break;

						case DbDataType.BigInt:
						case DbDataType.Array:
						case DbDataType.Binary:
						case DbDataType.Text:
							Write(param.DbValue.GetInt64());
							break;

						case DbDataType.Decimal:
						case DbDataType.Numeric:
							Write(
								param.DbValue.GetDecimal(),
								param.DataType,
								param.NumericScale);
							break;

						case DbDataType.Float:
							Write(param.DbValue.GetFloat());
							break;

						case DbDataType.Guid:
							WriteOpaque(param.DbValue.GetGuid().ToByteArray());
							break;

						case DbDataType.Double:
							Write(param.DbValue.GetDouble());
							break;

						case DbDataType.Date:
							Write(param.DbValue.GetDate());
							break;

						case DbDataType.Time:
							Write(param.DbValue.GetTime());
							break;

						case DbDataType.TimeStamp:
							Write(param.DbValue.GetDate());
							Write(param.DbValue.GetTime());
							break;

						case DbDataType.Boolean:
							Write(Convert.ToBoolean(param.Value));
							break;

						default:
							throw IscException.ForStrParam($"Unknown sql data type: {param.DataType}.");
					}
				}

				Write(param.NullFlag);
			}
			catch (IOException ex)
			{
				throw IscException.ForErrorCode(IscCodes.isc_net_write_err, ex);
			}
		}

		#endregion

		#region Private Methods

		private void CheckDisposed()
		{
			if (_innerStream == null)
				throw new ObjectDisposedException($"The {nameof(XdrStream)} is closed.");
		}

		private void ResetOperation()
		{
			_operation = -1;
		}

		#endregion

		#region Private Properties

		private bool ValidOperationAvailable
		{
			get { return _operation >= 0; }
		}

		#endregion
	}
}
