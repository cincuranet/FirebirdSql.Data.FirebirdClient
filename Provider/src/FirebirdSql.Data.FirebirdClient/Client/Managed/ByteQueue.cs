/*
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

//$Authors = Daniel Trubač

using System;
using System.IO;
using System.Threading.Tasks;

namespace FirebirdSql.Data.Client.Managed
{
	public class ByteQueue
	{
		protected int allocatedSize = 4096;
		protected byte[] buffer = null;
		protected int readIndex = 0;
		protected int writeIndex = 0;

		public ByteQueue(string name)
		{
			this.Name = name;
			this.buffer = new byte[allocatedSize];
		}

		public ByteQueue(string name, int size) : this(name)
		{
			allocatedSize = size;
			this.buffer = new byte[allocatedSize];
		}

		public int Count { get { return writeIndex - readIndex; } }
		protected string Name { get; }

		public void Add(byte data)
		{
			Resize(1);

			buffer[writeIndex] = data;
			writeIndex += 1;
		}

		public void Add(byte[] data, int count)
		{
			this.Add(data, count, 0);
		}

		public void Add(byte[] data, int count, int offset)
		{
			Resize(count);

			Buffer.BlockCopy(data, offset, buffer, writeIndex, count);
			writeIndex += count;
		}

		public void Clear()
		{
			this.readIndex = this.writeIndex = 0;
		}

		public int Get(byte[] data, int offset, int count)
		{
			if (this.Count < count)
				count = this.Count;

			Buffer.BlockCopy(this.buffer, readIndex, data, offset, count);
			readIndex += count;

			if (readIndex == writeIndex)
				readIndex = writeIndex = 0;

			return count;
		}

		public Int32 GetInt32()
		{
			if (this.Count < 4)
				throw new InvalidOperationException($"Not enough data in buffer, name: {this.Name}, GetInt32, rI:{readIndex}, wI:{writeIndex}");

			Int32 result = BitConverter.ToInt32(this.buffer, readIndex);
			readIndex += 4;

			if (readIndex == writeIndex)
				readIndex = writeIndex = 0;

			return result;
		}

		public Int64 GetInt64()
		{
			if (this.Count < 8)
				throw new InvalidOperationException($"Not enough data in buffer, name: {this.Name}, GetInt64, rI:{readIndex}, wI:{writeIndex}");

			Int64 result = BitConverter.ToInt64(this.buffer, readIndex);
			readIndex += 8;

			if (readIndex == writeIndex)
				readIndex = writeIndex = 0;

			return result;
		}

		public int ReadFromStream(Stream stream, int count)
		{
			var length = allocatedSize - writeIndex;

			if (count > length)
				Resize(count);

			var read = stream.Read(buffer, writeIndex, count);
			writeIndex += read;
			return read;
		}

		public async Task<int> ReadFromStreamAsync(Stream stream, int count)
		{
			var length = allocatedSize - writeIndex;

			if (count > length)
				Resize(count);

			var read = await stream.ReadAsync(buffer, writeIndex, count);

			writeIndex += read;
			return read;
		}

		public void WriteToStream(Stream stream)
		{
			var length = this.Count;
			stream.Write(buffer, readIndex, length);
			readIndex += length;

			if (readIndex == writeIndex)
				readIndex = writeIndex = 0;
		}

		protected void Resize(long length)
		{
			if (allocatedSize - writeIndex < length)
			{
				allocatedSize *= 2;
				var newBuffer = new byte[allocatedSize];

				Buffer.BlockCopy(this.buffer, 0, newBuffer, 0, this.buffer.Length);
				this.buffer = newBuffer;

				Resize(length);
			}
		}
	}
}