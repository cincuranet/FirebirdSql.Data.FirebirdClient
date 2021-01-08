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

using System.Text;

namespace FirebirdSql.Data.Common
{
	internal abstract class DatabaseParameterBufferBase : ParameterBuffer
	{
		public DatabaseParameterBufferBase(int version)
		{
			Append(version);
		}

		public abstract void Append(int type, byte value);
		public abstract void Append(int type, short value);
		public abstract void Append(int type, int value);
		public abstract void Append(int type, byte[] buffer);

		public void Append(int type, string content) => Append(type, Encoding.Default.GetBytes(content));
	}
}
