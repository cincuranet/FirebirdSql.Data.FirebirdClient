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
 *                  All Rights Reserved.
 */

using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Storage.Internal
{
    public class FbConnectionSettings  
    {
        public FbConnectionSettings([NotNull] MigrationsSqlGeneratorDependencies dependencies,
            [NotNull] IFbOptions options) 
        {
           // _options = options;
        } 
        private static readonly ConcurrentDictionary<string, FbConnectionSettings> Settings
            = new ConcurrentDictionary<string, FbConnectionSettings>();

        private static FbConnectionStringBuilder _settingsCsb(FbConnectionStringBuilder csb)
        {
           
            return new FbConnectionStringBuilder
            {                
                Database = csb.Database,
                Port = csb.Port,
                DataSource = csb.DataSource
            };
        }

        public static FbConnectionSettings GetSettings(string connectionString)
        {
            var csb = new FbConnectionStringBuilder(connectionString);
            var settingsCsb = _settingsCsb(csb);
            return Settings.GetOrAdd(settingsCsb.ConnectionString, key =>
            {
                csb.Pooling = false;
                string serverVersion=string.Empty;
                //Error connection database not exists
                try
                {
                    using (var _connection = new FbConnection(csb.ConnectionString))
                    {
                        _connection.Open();
                        serverVersion = _connection.ServerVersion;
                    }
                }
                catch 
                { } 
                var version = new ServerVersion(serverVersion);
                return new FbConnectionSettings(settingsCsb, version);
            });
        }

        public static FbConnectionSettings GetSettings(DbConnection connection)
        {
            var csb = new FbConnectionStringBuilder(connection.ConnectionString);
            var settingsCsb = _settingsCsb(csb);
            return Settings.GetOrAdd(settingsCsb.ConnectionString, key =>
            {
                var open = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    open = true;
                }
                try
                {
                    var version = new ServerVersion(connection.ServerVersion);
                    var connectionSettings = new FbConnectionSettings(settingsCsb, version);
                    return connectionSettings;
                }
                finally
                {
                    if (open)
                        connection.Close();
                }
            });
        }

        public FbConnectionSettings(FbConnectionStringBuilder settingsCsb, ServerVersion serverVersion)
        => ServerVersion = serverVersion;  

        public readonly ServerVersion ServerVersion;

    }
}
