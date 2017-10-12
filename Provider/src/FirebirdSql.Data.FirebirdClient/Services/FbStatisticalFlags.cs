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

namespace FirebirdSql.Data.Services
{
	[Flags]
	public enum FbStatisticalFlags
	{
		DataPages = 0x01,
		DatabaseLog = 0x02,
		HeaderPages = 0x04,
		IndexPages = 0x08,
		SystemTablesRelations = 0x10,
	}
}
