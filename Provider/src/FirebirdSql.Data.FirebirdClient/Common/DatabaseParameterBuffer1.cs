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

namespace FirebirdSql.Data.Common
{
	internal sealed class DatabaseParameterBuffer1 : DatabaseParameterBufferBase
	{
		public DatabaseParameterBuffer1()
			: base(IscCodes.isc_dpb_version1)
		{ }

		public override void Append(int type, byte value)
		{
			WriteByte(type);
			WriteByte(1);
			Write(value);
		}

		public override void Append(int type, short value)
		{
			WriteByte(type);
			WriteByte(2);
			Write(value);
		}

		public override void Append(int type, int value)
		{
			WriteByte(type);
			WriteByte(4);
			Write(value);
		}

		public override void Append(int type, byte[] buffer)
		{
			WriteByte(type);
			WriteByte(buffer.Length);
			Write(buffer);
		}
	}
}
