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
using System.Linq;
using System.Text;

namespace FirebirdSql.Data.Services
{
	public abstract class FbTraceConfiguration
	{
		protected static string WriteBoolValue(bool b)
		{
			return b ? "true" : "false";
		}

		protected static string WriteString(string s)
		{
			return string.Format("'{0}'", s);
		}

		protected static string WriteNumber(int i)
		{
			return i.ToString();
		}

		protected static string WriteRegEx(string re)
		{
			return WriteString(re.Replace(@"\", @"\\").Replace("'", @"\'"));
		}
	}
}
