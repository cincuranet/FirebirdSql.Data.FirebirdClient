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
 *
 *
 *                              
 *                  All Rights Reserved.
 */


using System;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using FirebirdSql.Data.FirebirdClient;
using System.Linq;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Internal
{
    public class FbOptions : IFbOptions
    {

        private FbOptionsExtension _contextOptions; 
        private Lazy<FbConnectionSettings> _connectionSettings; 

        public virtual void Initialize(IDbContextOptions options)
        {
            _contextOptions = options.Extensions.OfType<FbOptionsExtension>().FirstOrDefault(); 
            _connectionSettings = new Lazy<FbConnectionSettings>(() =>
                {
                    if (_contextOptions.Connection != null)
                        return FbConnectionSettings.GetSettings(_contextOptions.Connection);
                    return FbConnectionSettings.GetSettings(_contextOptions.ConnectionString);
                }); 
        }

        public virtual void Validate(IDbContextOptions options)
        {
           //Removed Add Future!
        }

        public virtual FbConnectionSettings ConnectionSettings => _connectionSettings.Value;

        public virtual string GetCreateTable(ISqlGenerationHelper sqlGenerationHelper, string table, string schema)
        {
            if (_contextOptions.Connection != null)
                return GetCreateTable(_contextOptions.Connection, sqlGenerationHelper, table, schema);
            return GetCreateTable(_contextOptions.ConnectionString, sqlGenerationHelper, table, schema);
        }

        private static string GetCreateTable(string connectionString, ISqlGenerationHelper sqlGenerationHelper, string table, string schema)
        {
            using (var connection = new FbConnection(connectionString))
            {
                connection.Open();
                return ExecuteCreateTable(connection, sqlGenerationHelper, table, schema);
            }
        }

        private static string GetCreateTable(DbConnection connection, ISqlGenerationHelper sqlGenerationHelper, string table, string schema)
        {
            var open = false;
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                open = true;
            }
            try
            {
                return ExecuteCreateTable(connection, sqlGenerationHelper, table, schema);
            }
            finally
            {
                if (open)
                    connection.Close();
            }
        }
        /// <summary>
        /// Implementation for Scaffolding
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlGenerationHelper"></param>
        /// <param name="table"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        /// Credit: http://www.firebirdfaq.org/faq174/  
        private static string ExecuteCreateTable(DbConnection connection, ISqlGenerationHelper sqlGenerationHelper, string table, string schema)
        {
            using (var command = connection.CreateCommand())
            {
                //List Schema Table
                command.CommandText = $@"SELECT
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
                WHERE (COALESCE(RF.RDB$SYSTEM_FLAG, 0) = 0) AND RF.RDB$RELATION_NAME='{sqlGenerationHelper.DelimitIdentifier(table, schema)}'
                ORDER BY RF.RDB$FIELD_POSITION"; 

                var sbFieds = new StringBuilder(); 
                using (var rd = command.ExecuteReader())
                {
                    if (rd.HasRows)
                        if (rd.HasRows)
                        {
                            sbFieds.AppendLine($"CREATE TABLE {sqlGenerationHelper.DelimitIdentifier(table, schema)} (");
                            while (rd.Read())
                                sbFieds.AppendLine($"{rd["FIELD_NAME"].ToString()}   {(rd["FIELD_TYPE"].ToString() + " " + rd["FIELD_NULL"]?.ToString())},");

                            sbFieds.AppendLine(");");
                        } 
                } 
                //List Primary Keys 
                command.CommandText = $@"
                    SELECT
                        IDX.RDB$INDEX_NAME, IDX.RDB$RELATION_NAME  
                    FROM RDB$INDICES IDX WHERE
                        IDX.RDB$RELATION_NAME = '{sqlGenerationHelper.DelimitIdentifier(table, schema)}'
                        AND LEFT(IDX.RDB$INDEX_NAME,3)='PK_'
                    GROUP BY DX.RDB$INDEX_NAME, IDX.RDB$RELATION_NAME"; 

                using (var rd = command.ExecuteReader())
                {
                    if (rd.HasRows)
                        while (rd.Read())
                        {
                            sbFieds.Append($"ALTER TABLE {sqlGenerationHelper.DelimitIdentifier(table, schema)} ");
                            sbFieds.Append($"ADD CONSTRAINT {rd["RDB$INDEX_NAME"].ToString()} ");
                            sbFieds.Append($"PRIMARY KEY ({rd["RDB$RELATION_NAME"].ToString()}) ");
                        }
                } 
                 
                // List Indexes
                command.CommandText = $@"
                      SELECT IDX.RDB$INDEX_NAME,
		                     IDX.RDB$UNIQUE_FLAG,
		                     IDX.RDB$RELATION_NAME
	                    FROM RDB$INDICES IDX WHERE
	                       IDX.RDB$RELATION_NAME = '{sqlGenerationHelper.DelimitIdentifier(table, schema)}'
	                       AND LEFT(IDX.RDB$INDEX_NAME,3)='PK_'
	                       AND LEFT(IDX.RDB$INDEX_NAME,3)<>'FK_'
	                    GROUP BY
	                       IDX.RDB$INDEX_NAME, IDX.RDB$UNIQUE_FLAG, IDX.RDB$RELATION_NAME";

                using (var rd = command.ExecuteReader())
                {
                    if(rd.HasRows)
                        while (rd.Read())
                        {
                            sbFieds.Append($"CREATE INDEX {rd["RDB$INDEX_NAME"].ToString()} ");
                            sbFieds.Append($"ON {sqlGenerationHelper.DelimitIdentifier(table, schema)} ");
                            sbFieds.Append($"({rd["RDB$RELATION_NAME"].ToString()})");
                        }
                } 
                return sbFieds.ToString(); 
            }
#pragma warning disable CS0162 
            return null;
#pragma warning restore CS0162 
        }

    }
}
