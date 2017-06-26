﻿/*
 *  Firebird ADO.NET Data provider for .NET and Mono
 *
 *     The contents of this file are subject to the Initial
 *     Developer's Public License Version 1.0 (the "License");
 *     you may not use this file except in compliance with the
 *     License. You may obtain a copy of the License at
 *     http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *     Software distributed under the License is distributed on
 *     an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *     express or implied.  See the License for the specific
 *     language governing rights and limitations under the License.
 *
 *  Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

using System;

namespace FirebirdSql.Data.FirebirdClient
{
#if !NETSTANDARD1_6
	[Serializable]
#endif
	public sealed class FbError
	{
		#region Fields

		private byte _classError;
		private int _lineNumber;
		private string _message;
		private int _number;

		#endregion

		#region Properties

		public byte Class
		{
			get { return _classError; }
		}

		public int LineNumber
		{
			get { return _lineNumber; }
		}

		public string Message
		{
			get { return _message; }
		}

		public int Number
		{
			get { return _number; }
		}

		#endregion

		#region Constructors

		internal FbError(string message, int number)
			: this(0, 0, message, number)
		{
		}

		internal FbError(byte classError, string message, int number)
			: this(classError, 0, message, number)
		{
		}

		internal FbError(byte classError, int line, string message, int number)
		{
			_classError = classError;
			_lineNumber = line;
			_number = number;
			_message = message;
		}

		#endregion
	}
}
