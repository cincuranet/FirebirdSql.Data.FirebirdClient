/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/raw/master/license.txt.
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

namespace FirebirdSql.Data.Client.Native.Marshalers;

internal unsafe struct XSQLVAR_STRUCT
{
	public short sqltype;
	public short sqlscale;
	public short sqlsubtype;
	public short sqllen;
	public IntPtr sqldata;
	public IntPtr sqlind;
	public short sqlname_length;
	public fixed byte sqlname[32];
	public short relname_length;
	public fixed byte relname[32];
	public short ownername_length;
	public fixed byte ownername[32];
	public short aliasname_length;
	public fixed byte aliasname[32];
}
