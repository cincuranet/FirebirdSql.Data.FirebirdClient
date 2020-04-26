/*
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

//$Authors = Jiri Cincura (jiri@cincura.net)
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.Logging;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Scaffolding.Internal
{

	public class FbDatabaseModelFactory : DatabaseModelFactory
	{
		Version _serverVersion;

		private readonly IDiagnosticsLogger<DbLoggerCategory.Scaffolding> _logger;

		public override DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options)
		{

			using var connection = new FbConnection(connectionString);
			return Create(connection, options);
		}

		public override DatabaseModel Create(DbConnection connection, DatabaseModelFactoryOptions options)
		{

			var databaseModel = new DatabaseModel();

			var connectionStartedOpen = connection.State == ConnectionState.Open;
			if (!connectionStartedOpen)
			{
				connection.Open();
			}

#warning Finish
			try
			{
				_serverVersion = Data.Services.FbServerProperties.ParseServerVersion(connection.ServerVersion);
				databaseModel.DatabaseName = connection.Database;
				databaseModel.DefaultSchema = GetDefaultSchema(connection);

				var schemaList = Enumerable.Empty<string>().ToList();
				var tableList = options.Tables.ToList();
				var tableFilter = GenerateTableFilter(tableList, schemaList);

				var tables = GetTables(connection, tableFilter);
				foreach (var table in tables)
				{
					table.Database = databaseModel;
					databaseModel.Tables.Add(table);
				}

				return databaseModel;
			}
			finally
			{
				if (!connectionStartedOpen)
				{
					connection.Close();
				}
			}
		}



		private string GetDefaultSchema(DbConnection connection)
		{
			return null;
		}

		private static Func<string, string, bool> GenerateTableFilter(
			IReadOnlyList<string> tables,
			IReadOnlyList<string> schemas)
		{
			return tables.Count > 0 ? (s, t) => tables.Contains(t) : (Func<string, string, bool>)null;
		}

		private const string GetTablesQuery = @"SELECT trim(r.RDB$RELATION_NAME),r.RDB$DESCRIPTION, r.RDB$RELATION_TYPE
FROM RDB$RELATIONS r
where r.RDB$SYSTEM_FLAG is distinct from 1
order by r.RDB$RELATION_NAME";

		private IEnumerable<DatabaseTable> GetTables(
			DbConnection connection,
			Func<string, string, bool> filter)
		{
			using (var command = connection.CreateCommand())
			{
				var tables = new List<DatabaseTable>();
				command.CommandText = GetTablesQuery;
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var name = reader.GetString(0);
						var comment = reader.GetString(1);
						var type = reader.GetInt32(2);

						var table = Int32.Equals(type, 0)
							? new DatabaseTable()
							: new DatabaseView();

						table.Schema = null;
						table.Name = name;
						table.Comment = string.IsNullOrEmpty(comment) ? null : comment;

						if (filter?.Invoke(table.Schema, table.Name) ?? true)
						{
							tables.Add(table);
						}
					}
				}

				// This is done separately due to MARS property may be turned off
				GetColumns(connection, tables, filter);
				GetPrimaryKeys(connection, tables);
				GetIndexes(connection, tables, filter);
				GetConstraints(connection, tables);

				return tables;
			}
		}

		private const string GetColumnsQuery = @"SELECT
    trim(RF.RDB$FIELD_NAME) as COLUMN_NAME,
    rf.RDB$FIELD_POSITION as ORDINAL_POSITION,
    COALESCE(RF.RDB$DEFAULT_SOURCE, F.RDB$DEFAULT_SOURCE) as COLUMN_DEFAULT,
    COALESCE(RF.RDB$NULL_FLAG, 0)  as NOT_NULL,
    CASE Coalesce(F.RDB$FIELD_TYPE, 0)
    WHEN 7 THEN
        CASE F.RDB$FIELD_SUB_TYPE
        WHEN 0 THEN 'SMALLINT'
        ELSE 'DECIMAL'
        END
    WHEN 8 THEN
        CASE F.RDB$FIELD_SUB_TYPE
        WHEN 0 THEN 'INTEGER'
        ELSE 'DECIMAL'
        END
    WHEN 9 THEN 'QUAD'
    WHEN 10 THEN 'FLOAT'
    WHEN 12 THEN 'DATE'
    WHEN 13 THEN 'TIME'
    WHEN 14 THEN 'CHAR(' || (TRUNC(F.RDB$FIELD_LENGTH / CH.RDB$BYTES_PER_CHARACTER)) || ') '
    WHEN 16 THEN
        CASE F.RDB$FIELD_SUB_TYPE
        WHEN 0 THEN 'BIGINT'
        ELSE 'DECIMAL'
        END
    WHEN 27 THEN 'DOUBLE PRECISION'
    WHEN 35 THEN 'TIMESTAMP'
    WHEN 37 THEN 'VARCHAR(' || (TRUNC(F.RDB$FIELD_LENGTH / CH.RDB$BYTES_PER_CHARACTER)) || ')'
    WHEN 40 THEN 'CSTRING' || (TRUNC(F.RDB$FIELD_LENGTH / CH.RDB$BYTES_PER_CHARACTER)) || ')'
    WHEN 45 THEN 'BLOB_ID'
    WHEN 261 THEN 'BLOB SUB_TYPE ' || F.RDB$FIELD_SUB_TYPE 
    ELSE 'RDB$FIELD_TYPE: ' || F.RDB$FIELD_TYPE || '?'
    END as STORE_TYPE,
    ch.RDB$CHARACTER_SET_NAME as CHARACTER_SET_NAME,
    F.rdb$description as COLUMN_COMMENT,
    COALESCE(RF.RDB$IDENTITY_TYPE, 0)   as auto_generated
    
FROM
    RDB$RELATION_FIELDS RF
JOIN
    RDB$FIELDS F ON(F.RDB$FIELD_NAME = RF.RDB$FIELD_SOURCE)
LEFT OUTER JOIN
    RDB$CHARACTER_SETS CH ON(CH.RDB$CHARACTER_SET_ID = F.RDB$CHARACTER_SET_ID)
WHERE
    (trim(RF.RDB$RELATION_NAME) = '" + "{0}" + @"') AND(COALESCE(RF.RDB$SYSTEM_FLAG, 0) = 0)
ORDER BY
RF.RDB$FIELD_POSITION;";

		private void GetColumns(
			DbConnection connection,
			IReadOnlyList<DatabaseTable> tables,
			Func<string, string, bool> tableFilter)
		{
			foreach (var table in tables)
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = string.Format(GetColumnsQuery, table.Name);
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							String name = reader["COLUMN_NAME"].ToString();
							String defaultValue = reader["COLUMN_DEFAULT"].ToString();
							Boolean nullable = !Convert.ToBoolean(reader["NOT_NULL"]);
							Boolean autoGenerated = Convert.ToBoolean(reader["AUTO_GENERATED"]);
							String storeType = reader["STORE_TYPE"].ToString();
							// String dataType = reader["DATA_TYPE"].ToString();
							String charset = reader["CHARACTER_SET_NAME"].ToString();
							// String collation = reader["COLLATION_NAME"].ToString();
							// String columType = reader["COLUMN_TYPE"].ToString();
							// String extra = reader["EXTRA"].ToString();
							String comment = reader["COLUMN_COMMENT"].ToString();


							ValueGenerated valueGenerated = ValueGenerated.Never;

							if (autoGenerated)
							{
								valueGenerated = ValueGenerated.OnAdd;
							}

							// defaultValue = FilterClrDefaults(dataType, nullable, defaultValue);

							var column = new DatabaseColumn
							{
								Table = table,
								Name = name,
								StoreType = storeType,
								IsNullable = nullable,
								DefaultValueSql = CreateDefaultValueString(defaultValue),
								ValueGenerated = valueGenerated,
								Comment = string.IsNullOrEmpty(comment) ? null : comment,
								// [FbAnnotationNames.CharSet] = _settings.CharSet ? charset : null,
								// [FbAnnotationNames.Collation] = _settings.Collation ? collation : null,
							};

							table.Columns.Add(column);
						}
					}
				}
			}
		}



		private string CreateDefaultValueString(string defaultValue)
		{
			if (defaultValue == null)
			{
				return null;
			}
			if (defaultValue.StartsWith("default "))
			{
				return defaultValue.Remove(0, 8);
			}
			else
			{
				return null;
			}

		}

		private const string GetPrimaryQuery = @"SELECT
   trim(i.rdb$index_name) as index_name,
   trim(sg.rdb$field_name) as field_name
FROM
    RDB$INDICES i
    LEFT JOIN rdb$index_segments sg on i.rdb$index_name = sg.rdb$index_name 
    LEFT JOIN rdb$relation_constraints rc on rc.rdb$index_name = I.rdb$index_name 
WHERE
     rc.rdb$constraint_type = 'PRIMARY KEY'
    AND
     trim(i.rdb$relation_name) = '{0}'
    order by sg.RDB$FIELD_POSITION;";

		/// <remarks>
		/// Primary keys are handled as in <see cref="GetConstraints"/>, not here
		/// </remarks>
		private void GetPrimaryKeys(
			DbConnection connection,
			IReadOnlyList<DatabaseTable> tables)
		{
			foreach (var x in tables)
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = string.Format(GetPrimaryQuery, x.Name);

					using (var reader = command.ExecuteReader())
					{
						DatabasePrimaryKey index = null;
						while (reader.Read())
						{
							try
							{
								if (index == null)
									index = new DatabasePrimaryKey
									{
										Table = x,
										Name = reader.GetString(0).Trim()
									};
								index.Columns.Add(x.Columns.Single(y => y.Name == reader.GetString(1).Trim()));

							}

							catch (Exception ex)
							{
								_logger.Logger.LogError(ex, "Error assigning primary key for {table}.", x.Name);
							}
							x.PrimaryKey = index;
						}
					}
				}
			}
		}

		private const string GetIndexesQuery = @"SELECT
    trim(I.rdb$index_name) as Index_Name,
    COALESCE(I.rdb$unique_flag, 0) as Non_Unique,
    list(trim(sg.RDB$FIELD_NAME)) as Columns
FROM
    RDB$INDICES i
    LEFT JOIN rdb$index_segments sg on i.rdb$index_name = sg.rdb$index_name
    LEFT JOIN rdb$relation_constraints rc on rc.rdb$index_name = I.rdb$index_name and rc.rdb$constraint_type = null
 WHERE
   trim(i.rdb$relation_name) = '{0}'
GROUP BY
   Index_Name, Non_Unique ;";

		/// <remarks>
		/// Primary keys are handled as in <see cref="GetConstraints"/>, not here
		/// </remarks>
		private void GetIndexes(DbConnection connection, IReadOnlyList<DatabaseTable> tables, Func<string, string, bool> tableFilter)
		{
			foreach (var table in tables)
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = string.Format(GetIndexesQuery, table.Name);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							try
							{
								var index = new DatabaseIndex
								{
									Table = table,
									Name = reader.GetString(0).Trim(),
									IsUnique = !reader.GetBoolean(1),
								};

								foreach (var column in reader.GetString(2).Split(','))
								{
									index.Columns.Add(table.Columns.Single(y => y.Name == column.Trim()));
								}


								table.Indexes.Add(index);
							}
							catch (Exception ex)
							{
								_logger.Logger.LogError(ex, "Error assigning index for {table}.", table.Name);
							}
						}
					}
				}
			}
		}

		private const string GetConstraintsQuery = @"SELECT
    trim(drs.rdb$constraint_name) as CONSTRAINT_NAME,
    trim(drs.RDB$RELATION_NAME) as TABLE_NAME,
    trim(mrc.rdb$relation_name) AS REFERENCED_TABLE_NAME,
    /* LIST(distinct trim(dis.rdb$field_name)||'#'||dis.rdb$field_position,',') AS SRC_COLUMN_NAME,
       LIST(distinct trim(mis.rdb$field_name)||'#'||mis.rdb$field_position,',') AS PRINC_COLUMN_NAME, */
    ( select list(trim(di.rdb$field_name)||'|'||trim(mi.rdb$field_name))
       from rdb$index_segments di
       join rdb$index_segments mi on mi.RDB$FIELD_POSITION=di.RDB$FIELD_POSITION and mi.rdb$index_name = mrc.rdb$index_name
       where di.rdb$index_name = drs.rdb$index_name) as PAIRED_COLUMNS,
    rc.RDB$DELETE_RULE as DELETE_RULE
FROM
    rdb$relation_constraints drs
    left JOIN rdb$ref_constraints rc ON drs.rdb$constraint_name = rc.rdb$constraint_name
    left JOIN rdb$relation_constraints mrc ON rc.rdb$const_name_uq = mrc.rdb$constraint_name
    
WHERE
    drs.rdb$constraint_type = 'FOREIGN KEY'
    AND trim(drs.RDB$RELATION_NAME) = '{0}' ";

		private void GetConstraints(DbConnection connection, IReadOnlyList<DatabaseTable> tables)
		{
			foreach (var table in tables)
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText = string.Format(GetConstraintsQuery, table.Name);
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var referencedTableName = reader.GetString(2);
							var referencedTable = tables.FirstOrDefault(t => t.Name == referencedTableName);
							if (referencedTable != null)
							{
								var fkInfo = new DatabaseForeignKey { Name = reader.GetString(0), OnDelete = ConvertToReferentialAction(reader.GetString(4)), Table = table, PrincipalTable = referencedTable };
								foreach (var pair in reader.GetString(3).Split(','))
								{
									fkInfo.Columns.Add(table.Columns.Single(y =>
										string.Equals(y.Name, pair.Split('|')[0], StringComparison.OrdinalIgnoreCase)));
									fkInfo.PrincipalColumns.Add(fkInfo.PrincipalTable.Columns.Single(y =>
										string.Equals(y.Name, pair.Split('|')[1], StringComparison.OrdinalIgnoreCase)));
								}

								table.ForeignKeys.Add(fkInfo);
							}
							else
							{
								_logger.Logger.LogWarning($"Referenced table `{referencedTableName}` is not in dictionary.");
							}
						}
					}
				}
			}
		}

		private static ReferentialAction? ConvertToReferentialAction(string onDeleteAction)
		{
			switch (onDeleteAction.ToUpperInvariant())
			{
				case "RESTRICT":
					return ReferentialAction.Restrict;

				case "CASCADE":
					return ReferentialAction.Cascade;

				case "SET NULL":
					return ReferentialAction.SetNull;

				case "NO ACTION":
					return ReferentialAction.NoAction;

				default:
					return null;
			}
		}
	}
}
