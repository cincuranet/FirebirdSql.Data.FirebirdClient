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

using FirebirdSql.Data.Common;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdSql.Data.Schema
{
	internal class FbProcedureParameters : FbSchema
	{
		#region Protected Methods

		protected override StringBuilder GetCommandText(string[] restrictions)
		{
			StringBuilder sql = new StringBuilder();
			StringBuilder where = new StringBuilder();

			sql.Append(
				@"SELECT
					null AS PROCEDURE_CATALOG,
					null AS PROCEDURE_SCHEMA,
					pp.rdb$procedure_name AS PROCEDURE_NAME,
					pp.rdb$parameter_name AS PARAMETER_NAME,
					null AS PARAMETER_DATA_TYPE,
					fld.rdb$field_sub_type AS PARAMETER_SUB_TYPE,
					pp.rdb$parameter_number AS ORDINAL_POSITION,
					CAST(pp.rdb$parameter_type AS integer) AS PARAMETER_DIRECTION,
					CAST(fld.rdb$field_length AS integer) AS PARAMETER_SIZE,
					CAST(fld.rdb$field_precision AS integer) AS NUMERIC_PRECISION,
					CAST(fld.rdb$field_scale AS integer) AS NUMERIC_SCALE,
					CAST(fld.rdb$character_length AS integer) AS CHARACTER_MAX_LENGTH,
					CAST(fld.rdb$field_length AS integer) AS CHARACTER_OCTET_LENGTH,
					coalesce(fld.rdb$null_flag, pp.rdb$null_flag) AS COLUMN_NULLABLE,
					null AS CHARACTER_SET_CATALOG,
					null AS CHARACTER_SET_SCHEMA,
					cs.rdb$character_set_name AS CHARACTER_SET_NAME,
					null AS COLLATION_CATALOG,
					null AS COLLATION_SCHEMA,
					coll.rdb$collation_name AS COLLATION_NAME,
					null AS COLLATION_CATALOG,
					null AS COLLATION_SCHEMA,
					pp.rdb$description AS DESCRIPTION,
					fld.rdb$field_type AS FIELD_TYPE
				FROM rdb$procedure_parameters pp
					LEFT JOIN rdb$fields fld ON pp.rdb$field_source = fld.rdb$field_name
					LEFT JOIN rdb$character_sets cs ON cs.rdb$character_set_id = fld.rdb$character_set_id
					LEFT JOIN rdb$collations coll ON (coll.rdb$collation_id = fld.rdb$collation_id AND coll.rdb$character_set_id = fld.rdb$character_set_id)");

			if (restrictions != null)
			{
				int index = 0;

				/* PROCEDURE_CATALOG */
				if (restrictions.Length >= 1 && restrictions[0] != null)
				{
				}

				/* PROCEDURE_SCHEMA	*/
				if (restrictions.Length >= 2 && restrictions[1] != null)
				{
				}

				/* PROCEDURE_NAME */
				if (restrictions.Length >= 3 && restrictions[2] != null)
				{
					where.AppendFormat(CultureInfo.CurrentCulture, "pp.rdb$procedure_name = @p{0}", index++);
				}

				/* PARAMETER_NAME */
				if (restrictions.Length >= 4 && restrictions[3] != null)
				{
					if (where.Length > 0)
					{
						where.Append(" AND ");
					}

					where.AppendFormat(CultureInfo.CurrentCulture, "pp.rdb$parameter_name = @p{0}", index++);
				}
			}

			if (where.Length > 0)
			{
				sql.AppendFormat(CultureInfo.CurrentCulture, " WHERE {0} ", where.ToString());
			}

			sql.Append(" ORDER BY pp.rdb$procedure_name, pp.rdb$parameter_type, pp.rdb$parameter_number");

			return sql;
		}

		protected override DataTable ProcessResult(DataTable schema)
		{
			schema.BeginLoadData();
			schema.Columns.Add("IS_NULLABLE", typeof(bool));

			foreach (DataRow row in schema.Rows)
			{
				int blrType = Convert.ToInt32(row["FIELD_TYPE"], CultureInfo.InvariantCulture);

				int subType = 0;
				if (row["PARAMETER_SUB_TYPE"] != DBNull.Value)
				{
					subType = Convert.ToInt32(row["PARAMETER_SUB_TYPE"], CultureInfo.InvariantCulture);
				}

				int scale = 0;
				if (row["NUMERIC_SCALE"] != DBNull.Value)
				{
					scale = Convert.ToInt32(row["NUMERIC_SCALE"], CultureInfo.InvariantCulture);
				}

				row["IS_NULLABLE"] = (row["COLUMN_NULLABLE"] == DBNull.Value);

				FbDbType dbType = (FbDbType)TypeHelper.GetDbDataTypeFromBlrType(blrType, subType, scale);
				row["PARAMETER_DATA_TYPE"] = TypeHelper.GetDataTypeName((DbDataType)dbType).ToLower(CultureInfo.InvariantCulture);

				if (dbType == FbDbType.Char || dbType == FbDbType.VarChar)
				{
					row["PARAMETER_SIZE"] = row["CHARACTER_MAX_LENGTH"];
				}
				else
				{
					row["CHARACTER_OCTET_LENGTH"] = 0;
				}

				if (dbType == FbDbType.Binary || dbType == FbDbType.Text)
				{
					row["PARAMETER_SIZE"] = Int32.MaxValue;
				}

				if (row["NUMERIC_PRECISION"] == DBNull.Value)
				{
					row["NUMERIC_PRECISION"] = 0;
				}

				if ((dbType == FbDbType.Decimal || dbType == FbDbType.Numeric) &&
					(row["NUMERIC_PRECISION"] == DBNull.Value || Convert.ToInt32(row["NUMERIC_PRECISION"]) == 0))
				{
					row["NUMERIC_PRECISION"] = row["PARAMETER_SIZE"];
				}

				row["NUMERIC_SCALE"] = (-1) * scale;

				int direction = Convert.ToInt32(row["PARAMETER_DIRECTION"], CultureInfo.InvariantCulture);
				switch (direction)
				{
					case 0:
						row["PARAMETER_DIRECTION"] = ParameterDirection.Input;
						break;

					case 1:
						row["PARAMETER_DIRECTION"] = ParameterDirection.Output;
						break;
				}
			}

			schema.EndLoadData();
			schema.AcceptChanges();

			// Remove not more needed columns
			schema.Columns.Remove("COLUMN_NULLABLE");
			schema.Columns.Remove("FIELD_TYPE");
			schema.Columns.Remove("CHARACTER_MAX_LENGTH");

			return schema;
		}

		#endregion
	}
}
#endif
