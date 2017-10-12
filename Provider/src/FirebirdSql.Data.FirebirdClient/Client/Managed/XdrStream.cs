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
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Globalization;
using System.Linq;
using FirebirdSql.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace FirebirdSql.Data.Client.Managed
{
	internal class XdrStream : Stream
	{
		#region Constants

		private const int PreferredBufferSize = 32 * 1024;

		#endregion

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
		private bool _compression;
		private bool _ownsStream;

		private long _position;
		private List<byte> _outputBuffer;
		private ReadBuffer _inputBuffer;
		private Ionic.Zlib.ZlibCodec _deflate;
		private Ionic.Zlib.ZlibCodec _inflate;
		private byte[] _compressionBuffer;

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
			get { return _position; }
			set { throw new NotSupportedException(); }
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
			: this(new MemoryStream(), charset, false, true)
		{ }

		public XdrStream(byte[] buffer, Charset charset)
			: this(new MemoryStream(buffer), charset, false, true)
		{ }

		public XdrStream(Stream innerStream, Charset charset, bool compression, bool ownsStream)
			: base()
		{
			_innerStream = innerStream;
			_charset = charset;
			_compression = compression;
			_ownsStream = ownsStream;

			_position = 0;
			_outputBuffer = new List<byte>(PreferredBufferSize);
			_inputBuffer = new ReadBuffer(PreferredBufferSize);
			if (_compression)
			{
				_deflate = new Ionic.Zlib.ZlibCodec(Ionic.Zlib.CompressionMode.Compress);
				_inflate = new Ionic.Zlib.ZlibCodec(Ionic.Zlib.CompressionMode.Decompress);
				_compressionBuffer = new byte[1024 * 1024];
			}

			ResetOperation();
		}

		#endregion

		#region Stream methods

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_ownsStream)
				{
					_innerStream?.Dispose();
				}
				_innerStream = null;
				_charset = null;
			}
		}

		public override void Flush()
		{
			CheckDisposed();

			var buffer = _outputBuffer.ToArray();
			_outputBuffer.Clear();
			var count = buffer.Length;
			if (_compression)
			{
				_deflate.OutputBuffer = _compressionBuffer;
				_deflate.AvailableBytesOut = _compressionBuffer.Length;
				_deflate.NextOut = 0;
				_deflate.InputBuffer = buffer;
				_deflate.AvailableBytesIn = buffer.Length;
				_deflate.NextIn = 0;
				var rc = _deflate.Deflate(Ionic.Zlib.FlushType.Sync);
				if (rc != Ionic.Zlib.ZlibConstants.Z_OK)
					throw new IOException($"Error '{rc}' while compressing the data.");
				if (_deflate.AvailableBytesIn != 0)
					throw new IOException("Compression buffer too small.");
				buffer = _compressionBuffer;
				count = _deflate.NextOut;
			}
			_innerStream.Write(buffer, 0, count);
			_innerStream.Flush();
		}

		public override void SetLength(long length)
		{
			CheckDisposed();

			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin loc)
		{
			CheckDisposed();

			throw new NotSupportedException();
		}

		public override int ReadByte()
		{
			CheckDisposed();
			EnsureReadable();

			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckDisposed();
			EnsureReadable();

			if (_inputBuffer.Length < count)
			{
				var readBuffer = new byte[PreferredBufferSize];
				var read = _innerStream.Read(readBuffer, 0, readBuffer.Length);
				if (read != 0)
				{
					if (_compression)
					{
						_inflate.OutputBuffer = _compressionBuffer;
						_inflate.AvailableBytesOut = _compressionBuffer.Length;
						_inflate.NextOut = 0;
						_inflate.InputBuffer = readBuffer;
						_inflate.AvailableBytesIn = read;
						_inflate.NextIn = 0;
						var rc = _inflate.Inflate(Ionic.Zlib.FlushType.None);
						if (rc != Ionic.Zlib.ZlibConstants.Z_OK)
							throw new IOException($"Error '{rc}' while decompressing the data.");
						if (_inflate.AvailableBytesIn != 0)
							throw new IOException("Decompression buffer too small.");
						readBuffer = _compressionBuffer;
						read = _inflate.NextOut;
					}
					_inputBuffer.AddRange(readBuffer, read);
				}
			}
			var dataLength = _inputBuffer.ReadInto(ref buffer, offset, count);
			_position += dataLength;
			return dataLength;
		}
		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			CheckDisposed();
			EnsureReadable();

			if (_inputBuffer.Length < count)
			{
				var readBuffer = new byte[PreferredBufferSize];
				var read = await _innerStream.ReadAsync(readBuffer, 0, readBuffer.Length).ConfigureAwait(false);
				if (read != 0)
				{
					if (_compression)
					{
						_inflate.OutputBuffer = _compressionBuffer;
						_inflate.AvailableBytesOut = _compressionBuffer.Length;
						_inflate.NextOut = 0;
						_inflate.InputBuffer = readBuffer;
						_inflate.AvailableBytesIn = read;
						_inflate.NextIn = 0;
						var rc = _inflate.Inflate(Ionic.Zlib.FlushType.None);
						if (rc != Ionic.Zlib.ZlibConstants.Z_OK)
							throw new IOException($"Error '{rc}' while decompressing the data.");
						if (_inflate.AvailableBytesIn != 0)
							throw new IOException("Decompression buffer too small.");
						readBuffer = _compressionBuffer;
						read = _inflate.NextOut;
					}
					_inputBuffer.AddRange(readBuffer, read);
				}
			}
			var dataLength = _inputBuffer.ReadInto(ref buffer, offset, count);
			_position += dataLength;
			return dataLength;
		}

		public override void WriteByte(byte value)
		{
			CheckDisposed();
			EnsureWritable();

			_outputBuffer.Add(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			CheckDisposed();
			EnsureWritable();

			_outputBuffer.AddRange(new ArraySegment<byte>(buffer, offset, count));
		}

		public byte[] ToArray()
		{
			CheckDisposed();

			var memoryStream = _innerStream as MemoryStream;
			if (memoryStream == null)
				throw new InvalidOperationException();
			Flush();
			return memoryStream.ToArray();
		}

		#endregion

		#region Operation Identification Methods

		public int ReadOperation()
		{
			var op = ValidOperationAvailable ? _operation : ReadNextOperation();
			ResetOperation();
			return op;
		}

		/* loop	as long	as we are receiving	dummy packets, just
		 * throwing	them away--note	that if	we are a server	we won't
		 * be receiving	them, but it is	better to check	for	them at
		 * this	level rather than try to catch them	in all places where
		 * this	routine	is called
		 */
		public int ReadNextOperation()
		{
			do
			{
				_operation = ReadInt32();
			} while (_operation == IscCodes.op_dummy);

			return _operation;
		}
		public async Task<int> ReadNextOperationAsync()
		{
			do
			{
				_operation = await ReadInt32Async().ConfigureAwait(false);
			} while (_operation == IscCodes.op_dummy);

			return _operation;
		}

		public void SetOperation(int operation)
		{
			_operation = operation;
		}

		private void ResetOperation()
		{
			_operation = -1;
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
		public async Task<byte[]> ReadBytesAsync(int count)
		{
			var buffer = new byte[count];
			if (count > 0)
			{
				var toRead = count;
				var currentlyRead = -1;
				while (toRead > 0 && currentlyRead != 0)
				{
					toRead -= (currentlyRead = await ReadAsync(buffer, count - toRead, toRead).ConfigureAwait(false));
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
		public async Task<int> ReadInt32Async()
		{
			return IPAddress.HostToNetworkOrder(BitConverter.ToInt32(await ReadBytesAsync(4).ConfigureAwait(false), 0));
		}

		public long ReadInt64()
		{
			return IPAddress.HostToNetworkOrder(BitConverter.ToInt64(ReadBytes(8), 0));
		}

		public Guid ReadGuid()
		{
			return TypeDecoder.DecodeGuid(ReadOpaque(16));
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
			return date.Add(time);
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

		public bool ReadBoolean()
		{
			return TypeDecoder.DecodeBoolean(ReadOpaque(1));
		}

		public IscException ReadStatusVector()
		{
			IscException exception = null;
			bool eof = false;

			while (!eof)
			{
				int arg = ReadInt32();

				switch (arg)
				{
					case IscCodes.isc_arg_gds:
					default:
						int er = ReadInt32();
						if (er != 0)
						{
							if (exception == null)
							{
								exception = IscException.ForBuilding();
							}
							exception.Errors.Add(new IscError(arg, er));
						}
						break;

					case IscCodes.isc_arg_end:
						exception?.BuildExceptionData();
						eof = true;
						break;

					case IscCodes.isc_arg_interpreted:
					case IscCodes.isc_arg_string:
						exception.Errors.Add(new IscError(arg, ReadString()));
						break;

					case IscCodes.isc_arg_number:
						exception.Errors.Add(new IscError(arg, ReadInt32()));
						break;

					case IscCodes.isc_arg_sql_state:
						exception.Errors.Add(new IscError(arg, ReadString()));
						break;
				}
			}

			return exception;
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
			WriteOpaque(TypeEncoder.EncodeBoolean(value));
		}

		public void Write(DateTime value)
		{
			WriteDate(value);
			WriteTime(TypeHelper.DateTimeToTimeSpan(value));
		}

		public void Write(Guid value)
		{
			WriteOpaque(TypeEncoder.EncodeGuid(value));
		}

		public void WriteDate(DateTime value)
		{
			Write(TypeEncoder.EncodeDate(Convert.ToDateTime(value)));
		}

		public void WriteTime(TimeSpan value)
		{
			Write(TypeEncoder.EncodeTime(value));
		}

		#endregion

		#region Private Methods

		private void CheckDisposed()
		{
			if (_innerStream == null)
				throw new ObjectDisposedException($"The {nameof(XdrStream)} is closed.");
		}

		private void EnsureWritable()
		{
			if (!CanWrite)
				throw new InvalidOperationException("Write operations are not allowed by this stream.");
		}

		private void EnsureReadable()
		{
			if (!CanRead)
				throw new InvalidOperationException("Read operations are not allowed by this stream.");
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
