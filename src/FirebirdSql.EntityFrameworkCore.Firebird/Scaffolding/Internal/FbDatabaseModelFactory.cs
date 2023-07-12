﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/raw/master/license.txt.
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
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Services;
using FirebirdSql.EntityFrameworkCore.Firebird.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.Logging;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Scaffolding.Internal;

public class FbDatabaseModelFactory : DatabaseModelFactory
{
	public int MajorVersionNumber { get; private set; }

	public override DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options)
	{
		using (var connection = new FbConnection(connectionString))
		{
			return Create(connection, options);
		}
	}

	public override DatabaseModel Create(DbConnection connection, DatabaseModelFactoryOptions options)
	{
		var databaseModel = new DatabaseModel();

		var connectionStartedOpen = connection.State == ConnectionState.Open;
		if (!connectionStartedOpen)
		{
			connection.Open();
		}

		var serverVersion = FbServerProperties.ParseServerVersion(connection.ServerVersion);
		MajorVersionNumber = serverVersion.Major;

		try
		{
			databaseModel.DatabaseName = connection.Database;
			databaseModel.DefaultSchema = GetDefaultSchema(connection);

			var schemaList = new List<string>();
			var tableList = options.Tables.ToList();
			var tableFilter = GenerateTableFilter(tableList, schemaList);

			var tables = GetTables(connection, tableFilter);
			foreach (var table in tables)
			{
				table.Database = databaseModel;
				if (tableFilter.Invoke(table))
				{
					databaseModel.Tables.Add(table);
				}
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

	private static string GetDefaultSchema(DbConnection connection)
	{
		return null;
	}

	private static Func<DatabaseTable, bool> GenerateTableFilter(IReadOnlyList<string> tables, IReadOnlyList<string> schemas)
	{
		return tables.Any() ? x => tables.Contains(x.Name) : _ => true;
	}

	private const string GetTablesQuery =
		@"SELECT
                trim(r.RDB$RELATION_NAME),
                r.RDB$DESCRIPTION,
                r.RDB$RELATION_TYPE
              FROM
               RDB$RELATIONS r
             WHERE
              r.RDB$SYSTEM_FLAG is distinct from 1
             ORDER BY
              r.RDB$RELATION_NAME";

	private IEnumerable<DatabaseTable> GetTables(DbConnection connection, Func<DatabaseTable, bool> filter)
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

					var table = type == 0
						? new DatabaseTable()
						: new DatabaseView();

					table.Schema = null;
					table.Name = name;
					table.Comment = string.IsNullOrEmpty(comment) ? null : comment;

					tables.Add(table);
				}
			}

			GetColumns(connection, tables, filter);
			GetPrimaryKeys(connection, tables);
			GetIndexes(connection, tables, filter);
			GetConstraints(connection, tables);

			return tables;
		}
	}

	private void GetColumns(DbConnection connection, IReadOnlyList<DatabaseTable> tables, Func<DatabaseTable, bool> tableFilter)
	{
		var getColumnsQuery = $@"
		SELECT
			rf.rdb$field_name COLUMN_NAME,
			COALESCE(rf.rdb$null_flag, f.rdb$null_flag, 0) COLUMN_REQUIRED,

			CASE COALESCE(f.rdb$field_type, 0)
				WHEN 7 THEN CASE f.rdb$field_sub_type
								WHEN 1 THEN 'NUMERIC(' || (f.rdb$field_precision) || ',' || ABS(f.rdb$field_scale) || ')'
								WHEN 2 THEN 'DECIMAL(' || (f.rdb$field_precision) || ',' || ABS(f.rdb$field_scale) || ')'
								ELSE '?'
							END
				WHEN 8 THEN CASE f.rdb$field_sub_type
								WHEN 1 THEN 'NUMERIC(' || (f.rdb$field_precision) || ',' || ABS(f.rdb$field_scale) || ')'
								WHEN 2 THEN 'DECIMAL(' || (f.rdb$field_precision) || ',' || ABS(f.rdb$field_scale) || ')'
								ELSE '?'
							END
				WHEN 9 THEN 'QUAD'
				WHEN 10 THEN 'FLOAT'
				WHEN 12 THEN 'DATE'
				WHEN 13 THEN 'TIME'
				WHEN 14 THEN 'CHAR(' || (TRUNC(f.rdb$field_length / ch.rdb$bytes_per_character)) || ')'
				WHEN 16 THEN CASE f.rdb$field_sub_type
								 WHEN 0 THEN 'BIGINT'
								 WHEN 1 THEN 'NUMERIC(' || (f.rdb$field_precision) || ',' || ABS(f.rdb$field_scale) || ')'
								 WHEN 2 THEN 'DECIMAL(' || (f.rdb$field_precision) || ',' || ABS(f.rdb$field_scale) || ')'
								 ELSE '?'
							 END
				WHEN 23 THEN 'BOOLEAN'
				WHEN 24 THEN 'DECFLOAT'
				WHEN 25 THEN 'DECFLOAT'
				WHEN 26 THEN 'INT128'
				WHEN 27 THEN 'DOUBLE PRECISION'
				WHEN 28 THEN 'TIME WITH TIME ZONE'
				WHEN 29 THEN 'TIMESTAMP WITH TIME ZONE'
				WHEN 35 THEN 'TIMESTAMP'
				WHEN 37 THEN 'VARCHAR(' || (TRUNC(f.rdb$field_length / ch.rdb$bytes_per_character)) || ')'
				WHEN 40 THEN 'CSTRING(' || (TRUNC(f.rdb$field_length / ch.rdb$bytes_per_character)) || ')'
				WHEN 45 THEN 'BLOB_ID'
				WHEN 261 THEN 'BLOB SUB_TYPE ' || CASE f.rdb$field_sub_type
													  WHEN 0 THEN 'BINARY'
													  WHEN 1 THEN 'TEXT'
													  ELSE f.rdb$field_sub_type
												  END
				ELSE 'RDB$FIELD_TYPE: ' || F.RDB$FIELD_TYPE || '?'
			END COLUMN_STORE_TYPE,

			rf.rdb$field_source COLUMN_DOMAIN,
			COALESCE(F.rdb$character_length, 0) COLUMN_LENGTH,
			COALESCE(F.rdb$field_precision, 0) COLUMN_PRECISION,
			COALESCE(-F.rdb$field_scale, 0) COLUMN_SCALE,

			NULLIF(ch.rdb$character_set_name, d.rdb$character_set_name) as CHARACTER_SET_NAME,
			co.rdb$collation_name COLLATION_NAME,
			COALESCE(f.rdb$segment_length, 0) SEGMENT_LENGTH,

			COALESCE(rf.rdb$default_source, f.rdb$default_source) COLUMN_DEFAULT,
			f.rdb$computed_source COLUMN_COMPUTED_SOURCE,
			f.rdb$description COLUMN_COMMENT,

			COALESCE({(MajorVersionNumber < 3 ? "NULL" : "rf.rdb$identity_type")}, -1) IDENTITY_TYPE,
			COALESCE({(MajorVersionNumber < 3 ? "NULL" : "g.rdb$initial_value")}, 1) IDENTITY_START,
			COALESCE({(MajorVersionNumber < 3 ? "NULL" : "g.rdb$generator_increment")}, 1) IDENTITY_INCREMENT
		FROM
			rdb$relation_fields rf
			JOIN rdb$fields f ON f.rdb$field_name = rf.rdb$field_source 
			LEFT JOIN rdb$character_sets ch ON ch.rdb$character_set_id = f.rdb$character_set_id
			LEFT JOIN rdb$collations co ON co.rdb$character_set_id = f.rdb$character_set_id AND co.rdb$collation_id = rf.rdb$collation_id
			{(MajorVersionNumber < 3 ? "" : "LEFT JOIN rdb$generators g ON g.rdb$generator_name = rf.rdb$generator_name")}
			CROSS JOIN rdb$database d
		WHERE
			TRIM(rf.rdb$relation_name) = @pRelationName AND COALESCE(rf.rdb$system_flag, 0) = 0
		ORDER BY
			rf.rdb$field_position";

		foreach (var table in tables)
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = getColumnsQuery;
				command.Parameters.Add(new FbParameter("@pRelationName", table.Name));
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var name = reader["COLUMN_NAME"].ToString();
						var isNullable = !Convert.ToBoolean(reader["COLUMN_REQUIRED"]);
						var storeType = reader["COLUMN_STORE_TYPE"].ToString();

						var columnDomain = reader["COLUMN_DOMAIN"].ToString();
						var columnLength = Convert.ToInt32(reader["COLUMN_LENGTH"]);
						var columnPrecision = Convert.ToInt32(reader["COLUMN_PRECISION"]);
						var columnScale = Convert.ToInt32(reader["COLUMN_SCALE"]);

						var charset = reader["CHARACTER_SET_NAME"].ToString();
						var collation = reader["COLLATION_NAME"].ToString();
						var segmentSize = Convert.ToInt32(reader["SEGMENT_LENGTH"]);

						var defaultValue = reader["COLUMN_DEFAULT"].ToString();
						var computedSource = reader["COLUMN_COMPUTED_SOURCE"].ToString();
						var comment = reader["COLUMN_COMMENT"].ToString();

						var identityType = Convert.ToInt32(reader["IDENTITY_TYPE"]);
						var identityStart = Convert.ToInt32(reader["IDENTITY_START"]);
						var identityIncrement = Convert.ToInt32(reader["IDENTITY_INCREMENT"]);

						var column = new DatabaseColumn
						{
							Table = table,
							Name = name.Trim(),
							StoreType = storeType,
							IsNullable = isNullable,

							DefaultValueSql = string.IsNullOrEmpty(defaultValue) ? null : defaultValue.Remove(0, 8),
							ValueGenerated = identityType == -1 ? ValueGenerated.Never : ValueGenerated.OnAdd,
							Comment = string.IsNullOrEmpty(comment) ? null : comment,
							Collation = string.IsNullOrEmpty(collation) ? null : collation.Trim(),
							ComputedColumnSql = string.IsNullOrEmpty(computedSource) ? null : computedSource
						};

						if (segmentSize > 0)
						{
							column.SetAnnotation(FbAnnotationNames.BlobSegmentSize, segmentSize);
						}

						if (!string.IsNullOrEmpty(charset))
						{
							column.SetAnnotation(FbAnnotationNames.CharacterSet, charset.Trim());
						}

						if (!columnDomain.StartsWith("RDB$"))
						{
							column.SetAnnotation(FbAnnotationNames.DomainName, columnDomain);
						}

						if (identityType != -1)
						{
							column.SetAnnotation(FbAnnotationNames.IdentityType, identityType);
						}

						if (identityStart != 1)
						{
							column.SetAnnotation(FbAnnotationNames.IdentityStart, identityStart);
						}

						if (identityIncrement != 1)
						{
							column.SetAnnotation(FbAnnotationNames.IdentityIncrement, identityIncrement);
						}

						table.Columns.Add(column);
					}
				}
			}
		}
	}

	private const string GetPrimaryQuery =
	@"SELECT
           trim(i.rdb$index_name) as INDEX_NAME,
           trim(sg.rdb$field_name) as FIELD_NAME
          FROM
           RDB$INDICES i
           LEFT JOIN rdb$index_segments sg on i.rdb$index_name = sg.rdb$index_name
           LEFT JOIN rdb$relation_constraints rc on rc.rdb$index_name = I.rdb$index_name
          WHERE
           rc.rdb$constraint_type = 'PRIMARY KEY'
           AND trim(i.rdb$relation_name) = '{0}'
          ORDER BY sg.RDB$FIELD_POSITION;";

	private void GetPrimaryKeys(DbConnection connection, IReadOnlyList<DatabaseTable> tables)
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
						if (index == null)
						{
							index = new DatabasePrimaryKey
							{
								Table = x,
								Name = reader.GetString(0).Trim()
							};
						}
						index.Columns.Add(x.Columns.Single(y => y.Name == reader.GetString(1).Trim()));
						x.PrimaryKey = index;
					}
				}
			}
		}
	}

	private const string GetIndexesQuery =
		@"SELECT
               trim(I.rdb$index_name) as INDEX_NAME,
               COALESCE(I.rdb$unique_flag, 0) as IS_UNIQUE,
               Coalesce(I.rdb$index_type, 0) as IS_DESC,
               list(trim(sg.RDB$FIELD_NAME)) as COLUMNS
              FROM
               RDB$INDICES i
               LEFT JOIN rdb$index_segments sg on i.rdb$index_name = sg.rdb$index_name
               LEFT JOIN rdb$relation_constraints rc on rc.rdb$index_name = I.rdb$index_name and rc.rdb$constraint_type = null
              WHERE
               trim(i.rdb$relation_name) = '{0}'
              GROUP BY
               INDEX_NAME, IS_UNIQUE, IS_DESC ;";

	/// <remarks>
	/// Primary keys are handled as in <see cref="GetConstraints"/>, not here
	/// </remarks>
	private void GetIndexes(DbConnection connection, IReadOnlyList<DatabaseTable> tables, Func<DatabaseTable, bool> tableFilter)
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
						var index = new DatabaseIndex
						{
							Table = table,
							Name = reader.GetString(0).Trim(),
							IsUnique = reader.GetBoolean(1),
						};

						foreach (var column in reader.GetString(3).Split(','))
						{
							index.Columns.Add(table.Columns.Single(y => y.Name == column.Trim()));
						}

						if (reader.GetBoolean(2))
						{
							var isDescending = new bool[index.Columns.Count];
							isDescending.AsSpan().Fill(true);
							index.IsDescending = isDescending;
						}

						table.Indexes.Add(index);
					}
				}
			}
		}
	}

	private const string GetConstraintsQuery =
		@"SELECT
               trim(drs.rdb$constraint_name) as CONSTRAINT_NAME,
               trim(drs.RDB$RELATION_NAME) as TABLE_NAME,
               trim(mrc.rdb$relation_name) AS REFERENCED_TABLE_NAME,
               (select list(trim(di.rdb$field_name)||'|'||trim(mi.rdb$field_name))
                from
                 rdb$index_segments di
                 join rdb$index_segments mi on mi.RDB$FIELD_POSITION=di.RDB$FIELD_POSITION and mi.rdb$index_name = mrc.rdb$index_name
                where
                 di.rdb$index_name = drs.rdb$index_name) as PAIRED_COLUMNS,
               trim(rc.RDB$DELETE_RULE) as DELETE_RULE
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
						var referencedTable = tables.First(t => t.Name == referencedTableName);
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
				}
			}
		}
	}

	private static ReferentialAction? ConvertToReferentialAction(string onDeleteAction)
	{
		return onDeleteAction.ToUpperInvariant() switch
		{
			"RESTRICT" => ReferentialAction.Restrict,
			"CASCADE" => ReferentialAction.Cascade,
			"SET NULL" => ReferentialAction.SetNull,
			"NO ACTION" => ReferentialAction.NoAction,
			_ => null,
		};
	}
}
