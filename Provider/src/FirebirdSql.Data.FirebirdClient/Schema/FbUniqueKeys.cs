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

#if !NETSTANDARD1_6
using System;
using System.Data;
using System.Globalization;
using System.Text;

namespace FirebirdSql.Data.Schema
{
	internal class FbUniqueKeys : FbSchema
	{
		#region Protected Methods

		protected override StringBuilder GetCommandText(string[] restrictions)
		{
			StringBuilder sql = new StringBuilder();
			StringBuilder where = new StringBuilder();

			sql.Append(
				@"SELECT
					null AS TABLE_CATALOG,
					null AS TABLE_SCHEMA,
					rel.rdb$relation_name AS TABLE_NAME,
					seg.rdb$field_name AS COLUMN_NAME,
					seg.rdb$field_position AS ORDINAL_POSITION,
					rel.rdb$constraint_name AS UK_NAME
				FROM rdb$relation_constraints rel
					LEFT JOIN rdb$indices idx ON rel.rdb$index_name = idx.rdb$index_name
					LEFT JOIN rdb$index_segments seg ON idx.rdb$index_name = seg.rdb$index_name");

			where.Append("rel.rdb$constraint_type = 'UNIQUE'");

			if (restrictions != null)
			{
				int index = 0;

				/* TABLE_CATALOG */
				if (restrictions.Length >= 1 && restrictions[0] != null)
				{
				}

				/* TABLE_SCHEMA */
				if (restrictions.Length >= 2 && restrictions[1] != null)
				{
				}

				/* TABLE_NAME */
				if (restrictions.Length >= 3 && restrictions[2] != null)
				{
					where.AppendFormat(CultureInfo.CurrentUICulture, " AND rel.rdb$relation_name = @p{0}", index++);
				}
			}

			if (where.Length > 0)
			{
				sql.AppendFormat(CultureInfo.CurrentUICulture, " WHERE {0} ", where.ToString());
			}

			sql.Append(" ORDER BY rel.rdb$relation_name, rel.rdb$constraint_name, seg.rdb$field_position");

			return sql;
		}

		#endregion
	}
}
#endif
