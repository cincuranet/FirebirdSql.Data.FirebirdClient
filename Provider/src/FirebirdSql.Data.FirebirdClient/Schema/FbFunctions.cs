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
	internal class FbFunctions : FbSchema
	{
		#region Protected Methods

		protected override StringBuilder GetCommandText(string[] restrictions)
		{
			StringBuilder sql = new StringBuilder();
			StringBuilder where = new StringBuilder();

			sql.Append(
				@"SELECT
					null AS FUNCTION_CATALOG,
					null AS FUNCTION_SCHEMA,
					rdb$function_name AS FUNCTION_NAME,
					rdb$system_flag AS IS_SYSTEM_FUNCTION,
					rdb$function_type AS FUNCTION_TYPE,
					rdb$query_name AS QUERY_NAME,
					rdb$module_name AS FUNCTION_MODULE_NAME,
					rdb$entrypoint AS FUNCTION_ENTRY_POINT,
					rdb$return_argument AS RETURN_ARGUMENT,
					rdb$description AS DESCRIPTION
				FROM rdb$functions");

			if (restrictions != null)
			{
				int index = 0;

				/* FUNCTION_CATALOG	*/
				if (restrictions.Length >= 1 && restrictions[0] != null)
				{
				}

				/* FUNCTION_SCHEMA */
				if (restrictions.Length >= 2 && restrictions[1] != null)
				{
				}

				/* FUNCTION_NAME */
				if (restrictions.Length >= 3 && restrictions[2] != null)
				{
					where.AppendFormat(CultureInfo.CurrentCulture, "rdb$function_name = @p{0}", index++);
				}

				/* IS_SYSTEM_FUNCTION */
				if (restrictions.Length >= 4 && restrictions[3] != null)
				{
					if (where.Length > 0)
					{
						where.Append(" AND ");
					}

					where.AppendFormat(CultureInfo.CurrentCulture, "rdb$system_flag = @p{0}", index++);
				}
			}

			if (where.Length > 0)
			{
				sql.AppendFormat(CultureInfo.CurrentCulture, " WHERE {0} ", where.ToString());
			}

			sql.Append(" ORDER BY rdb$function_name");

			return sql;
		}

		protected override DataTable ProcessResult(DataTable schema)
		{
			schema.BeginLoadData();

			foreach (DataRow row in schema.Rows)
			{
				if (row["IS_SYSTEM_FUNCTION"] == DBNull.Value ||
					Convert.ToInt32(row["IS_SYSTEM_FUNCTION"], CultureInfo.InvariantCulture) == 0)
				{
					row["IS_SYSTEM_FUNCTION"] = false;
				}
				else
				{
					row["IS_SYSTEM_FUNCTION"] = true;
				}
			}

			schema.EndLoadData();
			schema.AcceptChanges();

			return schema;
		}

		#endregion
	}
}
#endif
