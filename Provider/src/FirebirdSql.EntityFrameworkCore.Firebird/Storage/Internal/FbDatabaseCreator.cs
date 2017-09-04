/*                 
 *            FirebirdSql.EntityFrameworkCore.Firebird
 *              https://www.firebirdsql.org/en/net-provider/ 
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Services;
using FirebirdSql.EntityFrameworkCore.Firebird.Utilities;

namespace Microsoft.EntityFrameworkCore.Storage.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class FbDatabaseCreator : RelationalDatabaseCreator
    {
        private readonly IFbRelationalConnection _connection;
        private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;
        private TimeSpan _RetryDelay;
        private TimeSpan _RetryTimeout;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public FbDatabaseCreator(
            [NotNull] RelationalDatabaseCreatorDependencies dependencies,
            [NotNull] IFbRelationalConnection connection,
            [NotNull] IRawSqlCommandBuilder rawSqlCommandBuilder)
            : base(dependencies)
        {
            _connection = connection;
            _rawSqlCommandBuilder = rawSqlCommandBuilder;
            _RetryDelay = TimeSpan.FromMilliseconds(500);
            _RetryTimeout = TimeSpan.FromMinutes(2);
        }

        public override void Create()
        {
            using (var masterConnection = _connection.CreateMasterConnection())
            {
                Dependencies.MigrationCommandExecutor
                            .ExecuteNonQuery(CreateCreateOperations(), masterConnection);
            }
            Exists(retryOnNotExists: true);
        }

        public override async Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var masterConnection = _connection.CreateMasterConnection())
            {
                await Dependencies.MigrationCommandExecutor.ExecuteNonQueryAsync(CreateCreateOperations(), masterConnection, cancellationToken).ConfigureAwait(false);
                ClearPool();
            }

            await ExistsAsync(retryOnNotExists: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        protected override bool HasTables()
            => (long)CreateHasTablesCommand().ExecuteScalar(Dependencies.Connection) != 0;

        protected override Task<bool> HasTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
            => Dependencies.ExecutionStrategyFactory.Create().ExecuteAsync(_connection,
                async (connection, ct) => (long)await CreateHasTablesCommand().ExecuteScalarAsync(connection, cancellationToken: ct).ConfigureAwait(false) != 0, cancellationToken);
		
        private IRelationalCommand CreateHasTablesCommand()
            => _rawSqlCommandBuilder
                .Build(@"select count(*) from rdb$relations where rdb$view_blr is null and (rdb$system_flag is null or rdb$system_flag = 0);");

        private IReadOnlyList<MigrationCommand> CreateCreateOperations()
        {
            var operations = new MigrationOperation[]
            {
                new FbCreateDatabaseOperation
                {
                    connectionStrBuilder = new FbConnectionStringBuilder(_connection.DbConnection.ConnectionString),
                    Name = _connection.DbConnection.Database
                }
            };
            return Dependencies.MigrationsSqlGenerator.Generate(operations);
        }

	    private IReadOnlyList<MigrationCommand> CreateDropOperations()
	    {
		    var operations = new MigrationOperation[]
		    {
			    new FbDropDatabaseOperation
			    {
				    ConnectionStringBuilder = new FbConnectionStringBuilder(_connection.DbConnection.ConnectionString)
			    }
		    };
		    return Dependencies.MigrationsSqlGenerator.Generate(operations);
	    }

		public override bool Exists()
            => Exists(retryOnNotExists: false);

        private bool Exists(bool retryOnNotExists)
            => Dependencies.ExecutionStrategyFactory.Create().Execute(DateTime.Now + _RetryTimeout, giveUp =>
                {
                    while (true)
                    {
                        try
                        {
                            if (_connection?.DbConnection?.State != System.Data.ConnectionState.Open)
                                _connection.DbConnection.Open();

                            _connection.DbConnection.Close();
                            return true;
                        }
                        catch (FbException e)
                        {
                            if (!retryOnNotExists && DatabaseNotExist(e))
                                return false;

                            if (DateTime.Now > giveUp || !RetryOnExistsFailure(e))
                                throw;

                            Thread.Sleep(_RetryDelay);
                        }
                    }
                });

        public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken))
            => ExistsAsync(retryOnNotExists: false, cancellationToken: cancellationToken);

        private Task<bool> ExistsAsync(bool retryOnNotExists, CancellationToken cancellationToken)
            => Dependencies.ExecutionStrategyFactory.Create().ExecuteAsync(DateTime.UtcNow + _RetryTimeout, async (giveUp, ct) =>
                {
                    while (true)
                    {
                        try
                        {
                            await _connection.DbConnection.OpenAsync(ct).ConfigureAwait(false);
                            _connection.DbConnection.Close();
                            return true;
                        }
                        catch (FbException e)
                        {
                            if (!retryOnNotExists && DatabaseNotExist(e))
                                return false;

                            if (DateTime.UtcNow > giveUp || !RetryOnExistsFailure(e))
                                throw;

                            await Task.Delay(_RetryDelay, ct).ConfigureAwait(false);
                        }
                    }
                }, cancellationToken);

        private static bool DatabaseNotExist(FbException exception) => exception.ErrorCode == (int)FbErrorCode.FbErrorAccessFile;

        private bool RetryOnExistsFailure(FbException exception)
        {
            if (exception.ErrorCode == (int)FbErrorCode.FbErrorNetworkConnection)
            {
                ClearPool();
                return true;
            }
            return false;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override void Delete()
        {
            ClearAllPools();
            FbConnection.DropDatabase(_connection.ConnectionString); 
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
	        await Dependencies.MigrationCommandExecutor
			        .ExecuteNonQueryAsync(CreateDropOperations(), _connection, cancellationToken); 
        }

        private IReadOnlyList<MigrationCommand> CreateDropCommands()
        {
            var operations = new MigrationOperation[]
            {
                new FbDropDatabaseOperation { ConnectionStringBuilder = new FbConnectionStringBuilder(_connection.DbConnection.ConnectionString)}
            };
            return Dependencies.MigrationsSqlGenerator.Generate(operations);
        }

        private static void ClearAllPools() => FbConnection.ClearAllPools();
        private void ClearPool() => FbConnection.ClearPool(_connection.DbConnection as FbConnection);
    }
}
