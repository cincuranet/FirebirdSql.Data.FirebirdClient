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
 *      Jiri Cincura (jiri@cincura.net)
 */

using System;
using System.IO;
using System.Text;

namespace FirebirdSql.Data.Common
{
	internal sealed class ServiceParameterBuffer : ParameterBuffer
	{
		public ServiceParameterBuffer()
			: base(BitConverter.IsLittleEndian)
		{ }

		public void Append(int type, byte value)
		{
			WriteByte(type);
			WriteByte(value);
		}

		public void Append(int type, int value)
		{
			WriteByte(type);
			Write(value);
		}

		public void Append(int type, string value)
		{
			Append(type, Encoding.UTF8.GetBytes(value));
		}

		public void Append(byte type, string value)
		{
			Append(type, Encoding.UTF8.GetBytes(value));
		}

		public void Append(int type, byte[] value)
		{
			WriteByte((byte)type);
			Write((short)value.Length);
			Write(value);
		}

		public void Append(byte type, byte[] value)
		{
			WriteByte(type);
			WriteByte((byte)value.Length);
			Write(value);
		}
	}
}
