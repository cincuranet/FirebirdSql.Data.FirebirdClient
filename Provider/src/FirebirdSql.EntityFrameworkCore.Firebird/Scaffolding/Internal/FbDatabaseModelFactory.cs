// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the 
// project root for license information. 

/*                 
 *            FirebirdSql.EntityFrameworkCore.Firebird
 *     
 *              https://www.firebirdsql.org/en/net-provider/ 
 *              
 *     Permission to use, copy, modify, and distribute this software and its
 *     documentation for any purpose, without fee, and without a written
 *     agreement is hereby granted, provided that the above copyright notice
 *     and this paragraph and the following two paragraphs appear in all copies. 
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
 *      Credits: Rafael Almeida (ralms@ralms.net)
 *                              Sergipe-Brazil
 *               Jean Ressouche (jean.ressouche@souchprod.com)
 *                              Paris-France
 *                              
 *                  All Rights Reserved.
 */

// Search reference
// https://firebirdsql.org/refdocs/langrefupd20-select.html
// http://www.firebirdfaq.org/faq174/
// https://firebirdsql.org/refdocs/langrefupd21-aggrfunc-list.html
// Credit Query Schema Table:
// Jean Ressouche: https://raw.githubusercontent.com/souchprod/FirebirdSql.EntityFrameworkCore.Firebird/master/src/EFCore.Firebird/Scaffolding/Internal/FbDatabaseModelFactory.cs

using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Logging;
using FirebirdSql.Data.FirebirdClient;

namespace Microsoft.EntityFrameworkCore.Scaffolding.Internal
{
    public class FbDatabaseModelFactory : IDatabaseModelFactory
    {

        FbConnection _connection;
        TableSelectionSet _tableSelectionSet;
        DatabaseModel _databaseModel;
        Dictionary<string, DatabaseTable> _tables;
        Dictionary<string, DatabaseColumn> _tableColumns;
        static string TableKey(DatabaseTable table) => TableKey(table.Name, table.Schema);
        static string TableKey(string name, string schema) => $"{name}";
        static string ColumnKey(DatabaseTable table, string columnName) => $"{TableKey(table)}.{columnName}";

        #region Declaration Query
        private readonly string GetTablesQuery = @"SELECT
	RDB$RELATION_NAME
FROM
	RDB$RELATIONS
WHERE 
	RDB$VIEW_BLR IS NULL AND (RDB$SYSTEM_FLAG IS NULL OR RDB$SYSTEM_FLAG = 0)";

        private readonly string Columns = @"SELECT
                    RF.RDB$RELATION_NAME,
                    RF.RDB$FIELD_NAME FIELD_NAME,
                    RF.RDB$FIELD_POSITION FIELD_POSITION,
                    CASE F.RDB$FIELD_TYPE
                    WHEN 7 THEN
                        CASE F.RDB$FIELD_SUB_TYPE
                        WHEN 0 THEN 'SMALLINT'
                        WHEN 1 THEN 'NUMERIC(' || F.RDB$FIELD_PRECISION || ', ' || (-F.RDB$FIELD_SCALE) || ')'
                        WHEN 2 THEN 'DECIMAL'
                        END
                    WHEN 8 THEN
                        CASE F.RDB$FIELD_SUB_TYPE
                        WHEN 0 THEN 'INTEGER'
                        WHEN 1 THEN 'NUMERIC('  || F.RDB$FIELD_PRECISION || ', ' || (-F.RDB$FIELD_SCALE) || ')'
                        WHEN 2 THEN 'DECIMAL'
                        END
                    WHEN 9 THEN 'QUAD'
                    WHEN 10 THEN 'FLOAT'
                    WHEN 12 THEN 'DATE'
                    WHEN 13 THEN 'TIME'
                    WHEN 14 THEN 'CHAR(' || (TRUNC(F.RDB$FIELD_LENGTH / CH.RDB$BYTES_PER_CHARACTER)) || ') '
                    WHEN 16 THEN
                        CASE F.RDB$FIELD_SUB_TYPE
                        WHEN 0 THEN 'BIGINT'
                        WHEN 1 THEN 'NUMERIC(' || F.RDB$FIELD_PRECISION || ', ' || (-F.RDB$FIELD_SCALE) || ')'
                        WHEN 2 THEN 'DECIMAL'
                        END
                    WHEN 27 THEN 'DOUBLE'
                    WHEN 35 THEN 'TIMESTAMP'
                    WHEN 37 THEN
                        IIF (COALESCE(f.RDB$COMPUTED_SOURCE,'')<>'',
                        'COMPUTED BY ' || CAST(f.RDB$COMPUTED_SOURCE AS VARCHAR(250)),
                        'VARCHAR(' || (TRUNC(F.RDB$FIELD_LENGTH / CH.RDB$BYTES_PER_CHARACTER)) || ')')
                    WHEN 40 THEN 'CSTRING' || (TRUNC(F.RDB$FIELD_LENGTH / CH.RDB$BYTES_PER_CHARACTER)) || ')'
                    WHEN 45 THEN 'BLOB_ID'
                    WHEN 261 THEN 'BLOB SUB_TYPE ' || F.RDB$FIELD_SUB_TYPE
                    ELSE 'RDB$FIELD_TYPE: ' || F.RDB$FIELD_TYPE || '?'
                    END FIELD_TYPE,
                    IIF(COALESCE(RF.RDB$NULL_FLAG, 0) = 0, NULL, 'NOT NULL') FIELD_NULL,
                    CH.RDB$CHARACTER_SET_NAME FIELD_CHARSET,
                    DCO.RDB$COLLATION_NAME FIELD_COLLATION,
                    COALESCE(RF.RDB$DEFAULT_SOURCE, F.RDB$DEFAULT_SOURCE) FIELD_DEFAULT,
                    F.RDB$VALIDATION_SOURCE FIELD_CHECK,
                    RF.RDB$DESCRIPTION FIELD_DESCRIPTION
                FROM RDB$RELATION_FIELDS RF
                JOIN RDB$FIELDS F ON (F.RDB$FIELD_NAME = RF.RDB$FIELD_SOURCE)
                LEFT OUTER JOIN RDB$CHARACTER_SETS CH ON (CH.RDB$CHARACTER_SET_ID = F.RDB$CHARACTER_SET_ID)
                LEFT OUTER JOIN RDB$COLLATIONS DCO ON ((DCO.RDB$COLLATION_ID = F.RDB$COLLATION_ID) AND (DCO.RDB$CHARACTER_SET_ID = F.RDB$CHARACTER_SET_ID))
                WHERE (COALESCE(RF.RDB$SYSTEM_FLAG, 0) = 0) AND RF.RDB$RELATION_NAME='{0}'
                ORDER BY RF.RDB$FIELD_POSITION";

        private readonly string GetPrimaryQuery = @"
SELECT I.RDB$INDEX_NAME AS INDEX_NAME,SG.RDB$FIELD_NAME AS FIELD_NAME
FROM RDB$INDICES I
    LEFT JOIN RDB$INDEX_SEGMENTS SG ON I.RDB$INDEX_NAME = SG.RDB$INDEX_NAME
    LEFT JOIN RDB$RELATION_CONSTRAINTS RC ON RC.RDB$INDEX_NAME = I.RDB$INDEX_NAME
WHERE RC.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY' AND I.RDB$RELATION_NAME = '{0}'";

        private readonly string GetIndexesQuery = @"
SELECT
    I.RDB$INDEX_NAME, COALESCE(I.RDB$UNIQUE_FLAG, 0) AS ISUNIQUE,
    I.RDB$RELATION_NAME FROM  RDB$INDICES I
    LEFT JOIN RDB$INDEX_SEGMENTS SG ON I.RDB$INDEX_NAME = SG.RDB$INDEX_NAME
    LEFT JOIN RDB$RELATION_CONSTRAINTS RC ON RC.RDB$INDEX_NAME = I.RDB$INDEX_NAME AND RC.RDB$CONSTRAINT_TYPE = NULL
WHERE I.RDB$RELATION_NAME = '{0}'
GROUP BY I.RDB$INDEX_NAME, ISUNIQUE, I.RDB$RELATION_NAME";

        private readonly string GetConstraintsQuery = @"
SELECT
    CONST.RDB$CONSTRAINT_NAME,RELCONST.RDB$RELATION_NAME,REF.RDB$DELETE_RULE,
    LIST(DISTINCT TRIM(IDX.RDB$FIELD_NAME)||'$'||IDX.RDB$FIELD_POSITION,',') COLUMN_NAME
FROM
    RDB$RELATION_CONSTRAINTS CONST
    LEFT JOIN RDB$INDEX_SEGMENTS IDX ON CONST.RDB$INDEX_NAME = IDX.RDB$INDEX_NAME
    LEFT JOIN RDB$REF_CONSTRAINTS REF ON CONST.RDB$CONSTRAINT_NAME = REF.RDB$CONSTRAINT_NAME
    LEFT JOIN RDB$RELATION_CONSTRAINTS RELCONST ON REF.RDB$CONST_NAME_UQ = RELCONST.RDB$CONSTRAINT_NAME
    LEFT JOIN RDB$INDEX_SEGMENTS IDXSEG ON RELCONST.RDB$INDEX_NAME = IDXSEG.RDB$INDEX_NAME
WHERE
    CONST.RDB$CONSTRAINT_TYPE = 'FOREIGN KEY' AND CONST.RDB$RELATION_NAME = '{0}'
GROUP BY CONST.RDB$CONSTRAINT_NAME, RELCONST.RDB$RELATION_NAME,REF.RDB$DELETE_RULE";
        #endregion

        public FbDatabaseModelFactory(IDiagnosticsLogger<DbLoggerCategory.Scaffolding> loggerFactory)
        => Logger = loggerFactory;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual IDiagnosticsLogger<DbLoggerCategory.Scaffolding> Logger { get; }

        void ResetState()
        {
            _connection = null;
            _tableSelectionSet = null;
            _databaseModel = new DatabaseModel();
            _tables = new Dictionary<string, DatabaseTable>();
            _tableColumns = new Dictionary<string, DatabaseColumn>(StringComparer.OrdinalIgnoreCase);
        }

        public DatabaseModel Create(string connectionString, IEnumerable<string> tables, IEnumerable<string> schemas)
        {
            using (var connection = new FbConnection(connectionString))
                return Create(connection, tables, schemas);
        }

        public DatabaseModel Create(DbConnection connection, IEnumerable<string> tables, IEnumerable<string> schemas)
        => Create(connection, new TableSelectionSet(tables, schemas));
         
        public DatabaseModel Create(DbConnection connection, TableSelectionSet tableSelectionSet)
        {
            ResetState();
            _connection = connection as FbConnection;
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            try
            {
                _tableSelectionSet = tableSelectionSet;

                _databaseModel.DatabaseName = _connection.Database;
                _databaseModel.DefaultSchema = null;
                GetTables();
                GetColumns();
                GetPrimaryKeys();
                GetIndexes();
                GetConstraints();
                Logger.Logger.LogDebug($"Finish :)");
                return _databaseModel;
            }
            finally
            {
                FbConnection.ClearPool(_connection);
                _connection.Dispose();
            }
        }
         
        private void GetTables()
        {
            using (var command = new FbCommand(GetTablesQuery, _connection))
            using (var rResult = command.ExecuteReader())
            {
                while (rResult.Read())
                {
                    var table = new DatabaseTable
                    {
                        Schema = null,
                        Name = rResult["RDB$RELATION_NAME"].ToString().Trim()
                    };
                    Logger.Logger.LogDebug($"Creating: { rResult["RDB$RELATION_NAME"].ToString().Trim()} Model");
                    if (_tableSelectionSet.Allows(table.Schema, table.Name))
                    {
                        _databaseModel.Tables.Add(table);
                        _tables[TableKey(table)] = table;
                    }
                }
            }
        }

        private void GetColumns()
        {
            foreach (var table in _tables)
            {
                using (var command = new FbCommand(string.Format(Columns, table.Key), _connection))
                using (var rResult = command.ExecuteReader())
                    while (rResult.Read())
                    {
                        var column = new DatabaseColumn
                        {
                            Table = table.Value,
                            Name = rResult["FIELD_NAME"].ToString().Trim(),
                            StoreType = rResult["FIELD_TYPE"].ToString().Trim(),
                            IsNullable = rResult["FIELD_NULL"].ToString().Trim() == "NULL",
                            DefaultValueSql = rResult["FIELD_DEFAULT"].ToString() == "" ? null : rResult["FIELD_DEFAULT"].ToString(),
                        };

                        if (!string.IsNullOrEmpty(rResult["FIELD_DESCRIPTION"].ToString()))
                            column.AddAnnotation("Description", rResult["FIELD_DESCRIPTION"].ToString().Trim().Replace("\n", "; "));

                        table.Value.Columns.Add(column);
                    }
            }
        }
         
        private void GetPrimaryKeys()
        {
            foreach (var table in _tables)
            {
                DatabasePrimaryKey index = null;
                using (var command = new FbCommand(string.Format(GetPrimaryQuery, table.Key.Replace("\"", "")), _connection))
                using (var rResult = command.ExecuteReader())
                    while (rResult.Read())
                    {
                        if (index == null)
                            index = new DatabasePrimaryKey
                            {
                                Table = table.Value,
                                Name = rResult.GetString(0).Trim()
                            };
                        index.Columns.Add(table.Value.Columns.Single(y => y.Name == rResult.GetString(1).Trim()));

                    }
                table.Value.PrimaryKey = index;
            }
        }
         
        private void GetIndexes()
        {
            foreach (var table in _tables)
            {
                DatabaseIndex index = null;
                using (var command = new FbCommand(string.Format(GetIndexesQuery, table.Key), _connection))
                {
                    using (var rResult = command.ExecuteReader())
                        while (rResult.Read())
                        {
                            try
                            {
                                if (index == null)
                                    index = new DatabaseIndex
                                    {
                                        Table = table.Value,
                                        Name = rResult.GetString(0).Trim(),
                                        IsUnique = !rResult.GetBoolean(1),
                                    };

                                foreach (var column in rResult.GetString(2).Trim().Split(','))
                                    index.Columns.Add(table.Value.Columns.Single(y => y.Name == column));
                                 
                                table.Value.Indexes.Add(index);
                            }
                            catch { }
                        }
                }
            }
        }

        private void GetConstraints()
        {
            foreach (var table in _tables)
            {
                using (var command = new FbCommand(string.Format(GetConstraintsQuery, table.Key), _connection))
                {
                    using (var rResult = command.ExecuteReader())
                        while (rResult.Read())
                        {
                            if (_tables.ContainsKey(table.Key))
                            {
                                var foreignkey = new DatabaseForeignKey
                                {
                                    Table = table.Value,
                                    Name = rResult["RDB$CONSTRAINT_NAME"].ToString().Trim(),
                                    OnDelete = ConvertToReferentialAction(rResult["RDB$DELETE_RULE"].ToString()),
                                    PrincipalTable = _tables[rResult["RDB$RELATION_NAME"].ToString().Trim()]
                                };

                                var foreignkeyCols = rResult["COLUMN_NAME"].ToString().Split(',');
                                var columns = new string[foreignkeyCols.Length];

                                foreach (var positionCol in foreignkeyCols)
                                {
                                    var split = positionCol.Split('$');
                                    columns[int.Parse(split[1])] = split[0];
                                }

                                foreach (var column in columns)
                                    foreignkey.Columns.Add(table.Value.Columns.Single(y => y.Name == column));

                                var foreignkeyColsSecundary = rResult["COLUMN_NAME"].ToString().Split(',');
                                var columnsSecundary = new string[foreignkeyColsSecundary.Length];

                                foreach (var positionCol in foreignkeyColsSecundary)
                                {
                                    var split = positionCol.Split('$');
                                    columnsSecundary[int.Parse(split[1])] = split[0];
                                }

                                foreach (var column in columnsSecundary)
                                    foreignkey.PrincipalColumns.Add(table.Value.Columns.Single(y => y.Name == column));

                                table.Value.ForeignKeys.Add(foreignkey);
                            }
                        }
                }
                
            }
        }

        private static ReferentialAction? ConvertToReferentialAction(string onDeleteAction)
        {
            switch (onDeleteAction.ToUpperInvariant())
            {
                case "SET NULL":
                    return ReferentialAction.SetNull;
                case "SET DEFAUT":
                    return ReferentialAction.Restrict;
                case "CASCADE":
                    return ReferentialAction.Cascade;
                case "NO ACTION":
                    return ReferentialAction.NoAction;
                default:
                    return null;
            }
        }
    }
}
