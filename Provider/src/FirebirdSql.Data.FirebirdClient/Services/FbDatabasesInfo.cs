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
 *	Copyright (c) 2015 Jiri Cincura (jiri@cincura.net)
 *	All Rights Reserved.
 */

using System;
using System.Collections.Generic;

namespace FirebirdSql.Data.Services
{
	public class FbDatabasesInfo
	{
		public int ConnectionCount { get; internal set; }

		private List<string> _databases;
		public IReadOnlyList<string> Databases
		{
			get
			{
				return _databases.AsReadOnly();
			}
		}

		internal FbDatabasesInfo()
		{
			_databases = new List<string>();
		}

		internal void AddDatabase(string database)
		{
			_databases.Add(database);
		}
	}
}
