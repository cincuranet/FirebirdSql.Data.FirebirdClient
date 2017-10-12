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

#if !NETSTANDARD1_6
using System;
using System.Data;
using System.Data.Common;

namespace FirebirdSql.Data.FirebirdClient
{
	public sealed class FbRowUpdatingEventArgs : RowUpdatingEventArgs
	{
		#region Properties

		public new FbCommand Command
		{
			get  { return (FbCommand)base.Command; }
			set  { base.Command = value; }
		}

		#endregion

		#region Constructors

		public FbRowUpdatingEventArgs(
			DataRow				row,
			IDbCommand			command,
			StatementType		statementType,
			DataTableMapping		tableMapping)
			: base(row, command, statementType, tableMapping)
		{
		}

		#endregion
	}
}
#endif
