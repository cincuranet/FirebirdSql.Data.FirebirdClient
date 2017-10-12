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

namespace FirebirdSql.Data.Services
{
	[Flags]
	public enum FbValidationFlags
	{
		ValidateDatabase = 0x01,
		SweepDatabase = 0x02,
		MendDatabase = 0x04,
		CheckDatabase = 0x10,
		IgnoreChecksum = 0x20,
		KillShadows = 0x40,
		Full = 0x80
	}
}
