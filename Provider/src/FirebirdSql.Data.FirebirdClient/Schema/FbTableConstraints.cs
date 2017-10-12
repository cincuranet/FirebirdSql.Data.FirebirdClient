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
	internal class FbTableConstraints : FbSchema
	{
		#region Protected Methods

		protected override StringBuilder GetCommandText(string[] restrictions)
		{
			StringBuilder sql = new StringBuilder();
			StringBuilder where = new StringBuilder();

			sql.Append(
				@"SELECT
					null AS CONSTRAINT_CATALOG,
					null AS CONSTRAINT_SCHEMA,
					rc.rdb$constraint_name AS CONSTRAINT_NAME,
					null AS TABLE_CATALOG,
					null AS TABLE_SCHEMA,
					rc.rdb$relation_name AS TABLE_NAME,
					rc.rdb$constraint_type AS CONSTRAINT_TYPE,
				    rc.rdb$deferrable AS IS_DEFERRABLE,
				    rc.rdb$initially_deferred AS INITIALLY_DEFERRED
				FROM rdb$relation_constraints rc");

			if (restrictions != null)
			{
				int index = 0;

				/* CONSTRAINT_CATALOG */
				if (restrictions.Length >= 1 && restrictions[0] != null)
				{
				}

				/* CONSTRAINT_SCHEMA */
				if (restrictions.Length >= 2 && restrictions[1] != null)
				{
				}

				/* CONSTRAINT_NAME */
				if (restrictions.Length >= 3 && restrictions[2] != null)
				{
					if (where.Length > 0)
					{
						where.Append(" AND ");
					}

					where.AppendFormat(CultureInfo.CurrentUICulture, "rc.rdb$constraint_name = @p{0}", index++);
				}

				/* TABLE_CATALOG */
				if (restrictions.Length >= 4 && restrictions[3] != null)
				{
				}

				/* TABLE_SCHEMA */
				if (restrictions.Length >= 5 && restrictions[4] != null)
				{
				}

				/* TABLE_NAME */
				if (restrictions.Length >= 6 && restrictions[5] != null)
				{
					if (where.Length > 0)
					{
						where.Append(" AND ");
					}

					where.AppendFormat(CultureInfo.CurrentUICulture, "rc.rdb$relation_name = @p{0}", index++);
				}

				/* CONSTRAINT_TYPE */
				if (restrictions.Length >= 7 && restrictions[6] != null)
				{
					if (where.Length > 0)
					{
						where.Append(" AND ");
					}

					where.AppendFormat(CultureInfo.CurrentUICulture, "rc.rdb$constraint_type = @p{0}", index++);
				}
			}

			if (where.Length > 0)
			{
				sql.AppendFormat(CultureInfo.CurrentUICulture, " WHERE {0} ", where.ToString());
			}

			sql.Append(" ORDER BY rc.rdb$relation_name, rc.rdb$constraint_name");

			return sql;
		}

		protected override string[] ParseRestrictions(string[] restrictions)
		{
			string[] parsed = restrictions;

			if (parsed != null)
			{
				if (parsed.Length == 7 && parsed[6] != null)
				{
					switch (parsed[6].ToString().ToUpper(CultureInfo.InvariantCulture))
					{
						case "UNIQUE":
							parsed[3] = "u";
							break;

						case "PRIMARY KEY":
							parsed[3] = "p";
							break;

						case "FOREIGN KEY":
							parsed[3] = "f";
							break;

						case "CHECK":
							parsed[3] = "c";
							break;
					}
				}
			}

			return parsed;
		}

		#endregion
	}
}
#endif
