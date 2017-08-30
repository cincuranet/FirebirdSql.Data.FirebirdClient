﻿/*
 *  Firebird ADO.NET Data provider for .NET and Mono
 *
 *     The contents of this file are subject to the Initial
 *     Developer's Public License Version 1.0 (the "License");
 *     you may not use this file except in compliance with the
 *     License. You may obtain a copy of the License at
 *     http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *     Software distributed under the License is distributed on
 *     an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *     express or implied.  See the License for the specific
 *     language governing rights and limitations under the License.
 *
 *  Copyright (c) 2011-2017 Jiri Cincura (jiri@cincura.net)
 *  All Rights Reserved.
 */

using System;
using System.IO;
using System.Net.Sockets;

namespace FirebirdSql.Data.Common
{
	internal static class Extensions
	{
		static bool TrySocketAction(Action action)
		{
			try
			{
				action();
				return true;
			}
			catch (SocketException)
			{
				return false;
			}
			catch (PlatformNotSupportedException)
			{
				return false;
			}
		}

		public static bool TrySetKeepAlive(this Socket socket, ulong time, ulong interval)
		{
			const int BytesPerLong = 4;
			const int BitsPerByte = 8;
			bool turnOn = time != 0 && interval != 0;
			ulong[] input = new[]
				{
					turnOn ? (ulong)1 : (ulong)0,
					time,
					interval
				};
			// tcp_keepalive struct
			byte[] inValue = new byte[3 * BytesPerLong];
			for (int i = 0; i < input.Length; i++)
			{
				inValue[i * BytesPerLong + 3] = (byte)(input[i] >> ((BytesPerLong - 1) * BitsPerByte) & 0xFF);
				inValue[i * BytesPerLong + 2] = (byte)(input[i] >> ((BytesPerLong - 2) * BitsPerByte) & 0xFF);
				inValue[i * BytesPerLong + 1] = (byte)(input[i] >> ((BytesPerLong - 3) * BitsPerByte) & 0xFF);
				inValue[i * BytesPerLong + 0] = (byte)(input[i] >> ((BytesPerLong - 4) * BitsPerByte) & 0xFF);
			}
			byte[] outValue = BitConverter.GetBytes(0);

			return TrySocketAction(() =>
			{
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, turnOn ? 1 : 0);
				socket.IOControl(IOControlCode.KeepAliveValues, inValue, outValue);
			});
		}

		public static bool TryEnableLoopbackFastPath(this Socket socket)
		{
			const int SIOLoopbackFastPath = -1744830448; //0x98000010;
			byte[] inValue = BitConverter.GetBytes(1);
			return TrySocketAction(() =>
			{
				socket.IOControl(SIOLoopbackFastPath, inValue, null);
			});
		}

		public static int AsInt(this IntPtr ptr)
		{
			return (int)ptr.ToInt64();
		}

		public static bool TryGetTarget<T>(this WeakReference weakReference, out T target) where T : class
		{
			target = (T)weakReference.Target;
			return target != null;
		}

		public static IntPtr ReadIntPtr(this BinaryReader self)
		{
			if (IntPtr.Size == sizeof(int))
			{
				return new IntPtr(self.ReadInt32());
			}
			else if (IntPtr.Size == sizeof(long))
			{
				return new IntPtr(self.ReadInt64());
			}
			else
			{
				throw new NotSupportedException();
			}
		}
	}

	public static class PublicExtensions
	{
		public static string ToHexString(this byte[] b)
		{
			return BitConverter.ToString(b).Replace("-", string.Empty);
		}
	}
}
