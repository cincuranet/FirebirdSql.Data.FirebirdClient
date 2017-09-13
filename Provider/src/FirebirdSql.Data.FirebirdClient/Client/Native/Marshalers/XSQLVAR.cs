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

//$Authors = Carlos Guzman Alvarez

using System;
using System.Runtime.InteropServices;

namespace FirebirdSql.Data.Client.Native.Marshalers
{
	[StructLayout(LayoutKind.Sequential)]
	internal class XSQLVAR
	{
		public short sqltype;
		public short sqlscale;
		public short sqlsubtype;
		public short sqllen;
		public IntPtr sqldata;
		public IntPtr sqlind;
		public short sqlname_length;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] sqlname;
		public short relname_length;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] relname;
		public short ownername_length;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] ownername;
		public short aliasname_length;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] aliasname;
	}
}
