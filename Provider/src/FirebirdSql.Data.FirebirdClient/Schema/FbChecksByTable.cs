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
using System.Text;
using System.Globalization;

namespace FirebirdSql.Data.Schema
{
	internal class FbChecksByTable : FbSchema
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
					chktb.rdb$constraint_name AS CONSTRAINT_NAME,
					chktb.rdb$relation_name AS TABLE_NAME,
					trig.rdb$trigger_source AS CHECK_CLAUSULE,
				    trig.rdb$description AS DESCRIPTION
				FROM rdb$relation_constraints chktb
				    INNER JOIN rdb$check_constraints chk ON (chktb.rdb$constraint_name = chk.rdb$constraint_name AND chktb.rdb$constraint_type = 'CHECK')
				    INNER JOIN rdb$triggers trig ON chk.rdb$trigger_name = trig.rdb$trigger_name");

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
					where.AppendFormat(CultureInfo.CurrentUICulture, "chktb.rdb$constraint_name = @p{0}", index++);
				}
			}

			if (where.Length > 0)
			{
				sql.AppendFormat(CultureInfo.CurrentUICulture, " WHERE {0} ", where.ToString());
			}

			sql.Append(" ORDER BY chktb.rdb$relation_name, chktb.rdb$constraint_name");

			return sql;
		}

		#endregion
	}
}
#endif
