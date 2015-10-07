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
 *	Copyright (c) 2015 Jiri Cincura (jiri@cincura.net)
 *	All Rights Reserved.
 */

using System;

namespace FirebirdSql.Data.Isql
{
	public class UnknownStatementEventArgs : EventArgs
	{
		public FbStatement Statement { get; private set; }
		public bool Handled { get; set; }
		public bool Ignore { get; set; }
		public SqlStatementType Type { get; set; }

		public UnknownStatementEventArgs(FbStatement statement)
		{
			Statement = statement;
			Handled = false;
			Ignore = false;
		}
	}
}
