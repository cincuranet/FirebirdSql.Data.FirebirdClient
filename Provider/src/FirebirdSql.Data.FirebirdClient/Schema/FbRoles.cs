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
 *
 *  Constributors:
 *      Jiri Cincura (jiri@cincura.net)
 */

#if !NETSTANDARD1_6
using System;
using System.Data;
using System.Globalization;
using System.Text;

namespace FirebirdSql.Data.Schema
{
	internal class FbRoles : FbSchema
	{
		#region Protected Methods

		protected override StringBuilder GetCommandText(string[] restrictions)
		{
			StringBuilder sql = new StringBuilder();
			StringBuilder where = new StringBuilder();

			sql.Append(
				@"SELECT
					rdb$role_name AS ROLE_NAME,
					rdb$owner_name AS OWNER_NAME
				FROM rdb$roles");

			if (restrictions != null)
			{
				int index = 0;

				if (restrictions.Length >= 1 && restrictions[0] != null)
				{
					where.AppendFormat(CultureInfo.CurrentUICulture, "rdb$role_name = @p{0}", index++);
				}
			}

			if (where.Length > 0)
			{
				sql.AppendFormat(CultureInfo.CurrentUICulture, " WHERE {0} ", where.ToString());
			}

			sql.Append(" ORDER BY rdb$role_name");

			return sql;
		}

		#endregion
	}
}
#endif