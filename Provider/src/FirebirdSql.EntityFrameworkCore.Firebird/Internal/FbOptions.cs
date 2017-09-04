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
					WHEN 7 THEN CASE
					WHEN ((F.RDB$FIELD_SUB_TYPE = 2) OR (F.RDB$FIELD_SUB_TYPE = 0 AND F.RDB$FIELD_SCALE > 0)) THEN 'DECIMAL'
					WHEN F.RDB$FIELD_SUB_TYPE = 1 THEN 'NUMERIC'
					ELSE 'SMALLINT'
					END
					WHEN 8 THEN CASE
					WHEN ((F.RDB$FIELD_SUB_TYPE = 2) OR (F.RDB$FIELD_SUB_TYPE = 0 AND F.RDB$FIELD_SCALE > 0)) THEN 'DECIMAL'
					WHEN F.RDB$FIELD_SUB_TYPE = 1 THEN 'NUMERIC'
					ELSE 'INT'
					END
					WHEN 16 THEN CASE
					WHEN ((F.RDB$FIELD_SUB_TYPE = 2) OR (F.RDB$FIELD_SUB_TYPE = 0 AND F.RDB$FIELD_SCALE > 0)) THEN 'DECIMAL'
					WHEN F.RDB$FIELD_SUB_TYPE = 1 THEN 'NUMERIC'
					ELSE 'BIGINT'
					END
					WHEN 10 THEN 'FLOAT'
					WHEN 27 THEN 'DOUBLE'
					WHEN 12 THEN 'DATE'
					WHEN 13 THEN 'TIME'
					WHEN 35 THEN 'TIMESTAMP'
					WHEN 261 THEN 'BLOB SUB_TYPE ' || F.RDB$FIELD_SUB_TYPE
					WHEN 37 THEN 'VARCHAR'
					WHEN 14 THEN 'CHAR'
					WHEN 40 THEN 'CSTRING'
					END) AS FIELD_TYPE,
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

                var builder = new StringBuilder(); 
                using (var rd = command.ExecuteReader())
                {
                    if (rd.HasRows)
                    {
						builder.AppendLine($"CREATE TABLE {sqlGenerationHelper.DelimitIdentifier(table, schema)} (");

						while (rd.Read())
							builder.AppendLine($"{rd["FIELD_NAME"]}   {($"{rd["FIELD_TYPE"].ToString() + (rd["FIELD_NULL"] == DBNull.Value? "": $" {rd["FIELD_NULL"]}") }")},");

						builder.AppendLine(");");
                    } 
                }
				
				//List Primary Keys
				command.CommandText = $@"
					SELECT I.RDB$INDEX_NAME, SG.RDB$FIELD_NAME
					FROM RDB$INDICES IDX
					LEFT JOIN RDB$INDEX_SEGMENTS SG ON IDX.RDB$INDEX_NAME = SG.RDB$INDEX_NAME
					LEFT JOIN RDB$RELATION_CONSTRAINTS RC ON RC.RDB$INDEX_NAME = IDX.RDB$INDEX_NAME
					WHERE RC.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'
					AND IDX.RDB$RELATION_NAME = '{sqlGenerationHelper.DelimitIdentifier(table, schema)}'";

                using (var rd = command.ExecuteReader())
                {
                    while (rd.Read())
                    {
						builder.Append($"ALTER TABLE {sqlGenerationHelper.DelimitIdentifier(table, schema)} ");
						builder.Append($"ADD CONSTRAINT {rd["RDB$INDEX_NAME"]} ");
						builder.Append($"PRIMARY KEY ({rd["RDB$FIELD_NAME"]}) ");
                    }
                }

				// List Indexes
				command.CommandText = $@"
					SELECT
						I.RDB$INDEX_NAME,
						COALESCE(I.RDB$UNIQUE_FLAG, 0) AS ISUNIQUE,
						I.RDB$RELATION_NAME
						FROM RDB$INDICES I
						LEFT JOIN RDB$INDEX_SEGMENTS SG ON I.RDB$INDEX_NAME = SG.RDB$INDEX_NAME
						LEFT JOIN RDB$RELATION_CONSTRAINTS RC ON RC.RDB$INDEX_NAME = I.RDB$INDEX_NAME AND RC.RDB$CONSTRAINT_TYPE = NULL
					WHERE I.RDB$RELATION_NAME = '{sqlGenerationHelper.DelimitIdentifier(table, schema)}'
					GROUP BY I.RDB$INDEX_NAME, ISUNIQUE, I.RDB$RELATION_NAME";

                using (var rd = command.ExecuteReader())
                {
                    while (rd.Read())
                    {
						builder.Append($"CREATE INDEX {rd["RDB$INDEX_NAME"]} ");
						builder.Append($"ON {sqlGenerationHelper.DelimitIdentifier(table, schema)} ");
						builder.Append($"({rd["RDB$RELATION_NAME"]})");
                    }
                } 
                return builder.ToString(); 
            }
        }

    }
}