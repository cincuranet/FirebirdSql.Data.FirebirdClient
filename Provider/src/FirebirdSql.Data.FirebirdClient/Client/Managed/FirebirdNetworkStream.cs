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

//$Authors = Jiri Cincura (jiri@cincura.net)

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed
{
#warning Does this need to be a stream?
	class FirebirdNetworkStream : Stream, ITracksIOFailure
	{
		public const string CompressionName = "zlib";
		public const string EncryptionName = "Arc4";

		const int PreferredBufferSize = 32 * 1024;

		readonly NetworkStream _networkStream;

		readonly Queue<byte> _outputBuffer;
		readonly Queue<byte> _inputBuffer;
		readonly byte[] _readBuffer;

		byte[] _compressionBuffer;
		Ionic.Zlib.ZlibCodec _compressor;
		Ionic.Zlib.ZlibCodec _decompressor;

		Org.BouncyCastle.Crypto.Engines.RC4Engine _decryptor;
		Org.BouncyCastle.Crypto.Engines.RC4Engine _encryptor;

		public FirebirdNetworkStream(NetworkStream networkStream)
		{
			_networkStream = networkStream;

			_outputBuffer = new Queue<byte>(PreferredBufferSize);
			_inputBuffer = new Queue<byte>(PreferredBufferSize);
			_readBuffer = new byte[PreferredBufferSize];
		}

		public bool IOFailed { get; set; }

		public override int Read(byte[] buffer, int offset, int count) => ReadImpl(buffer, offset, count, new AsyncWrappingCommonArgs(false)).GetAwaiter().GetResult();
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadImpl(buffer, offset, count, new AsyncWrappingCommonArgs(true, cancellationToken));
		private async Task<int> ReadImpl(byte[] buffer, int offset, int count, AsyncWrappingCommonArgs async)
		{
			if (_inputBuffer.Count < count)
			{
				var readBuffer = _readBuffer;
				int read;
				try
				{
					read = await async.AsyncSyncCall(_networkStream.ReadAsync, _networkStream.Read, readBuffer, 0, readBuffer.Length).ConfigureAwait(false);
				}
				catch (IOException)
				{
					IOFailed = true;
					throw;
				}
				if (read != 0)
				{
					if (_decryptor != null)
					{
						_decryptor.ProcessBytes(readBuffer, 0, read, readBuffer, 0);
					}
					if (_decompressor != null)
					{
						read = HandleDecompression(readBuffer, read);
						readBuffer = _compressionBuffer;
					}
					WriteToInputBuffer(readBuffer, read);
				}
			}
			var dataLength = ReadFromInputBuffer(buffer, offset, count);
			return dataLength;
		}

		public override void Write(byte[] buffer, int offset, int count) => WriteImpl(buffer, offset, count, new AsyncWrappingCommonArgs(false)).GetAwaiter().GetResult();
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteImpl(buffer, offset, count, new AsyncWrappingCommonArgs(true, cancellationToken));
		private Task WriteImpl(byte[] buffer, int offset, int count, AsyncWrappingCommonArgs async)
		{
			for (var i = offset; i < count; i++)
				_outputBuffer.Enqueue(buffer[offset + i]);
			return Task.CompletedTask;
		}

		public override void Flush() => FlushImpl(new AsyncWrappingCommonArgs(false)).GetAwaiter().GetResult();
		public override Task FlushAsync(CancellationToken cancellationToken) => FlushImpl(new AsyncWrappingCommonArgs(true, cancellationToken));
		private async Task FlushImpl(AsyncWrappingCommonArgs async)
		{
			var buffer = _outputBuffer.ToArray();
			_outputBuffer.Clear();
			var count = buffer.Length;
			if (_compressor != null)
			{
				count = HandleCompression(buffer, count);
				buffer = _compressionBuffer;
			}
			if (_encryptor != null)
			{
				_encryptor.ProcessBytes(buffer, 0, count, buffer, 0);
			}
			try
			{
				await async.AsyncSyncCall(_networkStream.WriteAsync, _networkStream.Write, buffer, 0, count).ConfigureAwait(false);
				await async.AsyncSyncCall(_networkStream.FlushAsync, _networkStream.Flush).ConfigureAwait(false);
			}
			catch (IOException)
			{
				IOFailed = true;
				throw;
			}
		}

		public void StartCompression()
		{
			_compressionBuffer = new byte[PreferredBufferSize];
			_compressor = new Ionic.Zlib.ZlibCodec(Ionic.Zlib.CompressionMode.Compress);
			_decompressor = new Ionic.Zlib.ZlibCodec(Ionic.Zlib.CompressionMode.Decompress);
		}

		public void StartEncryption(byte[] key)
		{
			_encryptor = CreateCipher(key);
			_decryptor = CreateCipher(key);
		}

		protected override void Dispose(bool disposing)
		{
			_networkStream.Dispose();
			base.Dispose(disposing);
		}
#if !(NET48 || NETSTANDARD2_0)
		public override async ValueTask DisposeAsync()
		{
			await _networkStream.DisposeAsync().ConfigureAwait(false);
			await base.DisposeAsync().ConfigureAwait(false);
		}
#endif

		int ReadFromInputBuffer(byte[] buffer, int offset, int count)
		{
			var read = Math.Min(count, _inputBuffer.Count);
			for (var i = 0; i < read; i++)
			{
				buffer[offset + i] = _inputBuffer.Dequeue();
			}
			return read;
		}

		void WriteToInputBuffer(byte[] data, int count)
		{
			for (var i = 0; i < count; i++)
			{
				_inputBuffer.Enqueue(data[i]);
			}
		}

		int HandleDecompression(byte[] buffer, int count)
		{
			_decompressor.InputBuffer = buffer;
			_decompressor.NextOut = 0;
			_decompressor.NextIn = 0;
			_decompressor.AvailableBytesIn = count;
			while (true)
			{
				_decompressor.OutputBuffer = _compressionBuffer;
				_decompressor.AvailableBytesOut = _compressionBuffer.Length - _decompressor.NextOut;
				var rc = _decompressor.Inflate(Ionic.Zlib.FlushType.None);
				if (rc != Ionic.Zlib.ZlibConstants.Z_OK)
					throw new IOException($"Error '{rc}' while decompressing the data.");
				if (_decompressor.AvailableBytesIn > 0 || _decompressor.AvailableBytesOut == 0)
				{
					ResizeBuffer(ref _compressionBuffer);
					continue;
				}
				break;
			}
			return _decompressor.NextOut;
		}

		int HandleCompression(byte[] buffer, int count)
		{
			_compressor.InputBuffer = buffer;
			_compressor.NextOut = 0;
			_compressor.NextIn = 0;
			_compressor.AvailableBytesIn = count;
			while (true)
			{
				_compressor.OutputBuffer = _compressionBuffer;
				_compressor.AvailableBytesOut = _compressionBuffer.Length - _compressor.NextOut;
				var rc = _compressor.Deflate(Ionic.Zlib.FlushType.None);
				if (rc != Ionic.Zlib.ZlibConstants.Z_OK)
					throw new IOException($"Error '{rc}' while compressing the data.");
				if (_compressor.AvailableBytesIn > 0 || _compressor.AvailableBytesOut == 0)
				{
					ResizeBuffer(ref _compressionBuffer);
					continue;
				}
				break;
			}
			while (true)
			{
				_compressor.OutputBuffer = _compressionBuffer;
				_compressor.AvailableBytesOut = _compressionBuffer.Length - _compressor.NextOut;
				var rc = _compressor.Deflate(Ionic.Zlib.FlushType.Sync);
				if (rc != Ionic.Zlib.ZlibConstants.Z_OK)
					throw new IOException($"Error '{rc}' while compressing the data.");
				if (_compressor.AvailableBytesIn > 0 || _compressor.AvailableBytesOut == 0)
				{
					ResizeBuffer(ref _compressionBuffer);
					continue;
				}
				break;
			}
			return _compressor.NextOut;
		}

		static void ResizeBuffer(ref byte[] buffer)
		{
			Array.Resize(ref buffer, buffer.Length * 2);
		}

		static Org.BouncyCastle.Crypto.Engines.RC4Engine CreateCipher(byte[] key)
		{
			var cipher = new Org.BouncyCastle.Crypto.Engines.RC4Engine();
			cipher.Init(default, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));
			return cipher;
		}

		public override bool CanRead => throw new NotSupportedException();
		public override bool CanSeek => throw new NotSupportedException();
		public override bool CanWrite => throw new NotSupportedException();
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
	}
}
