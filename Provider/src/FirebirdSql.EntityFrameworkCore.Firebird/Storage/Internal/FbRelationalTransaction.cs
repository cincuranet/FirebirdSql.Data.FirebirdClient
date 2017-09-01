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
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics; 
using Microsoft.EntityFrameworkCore.Internal; 
using FirebirdSql.Data.FirebirdClient;

namespace Microsoft.EntityFrameworkCore.Storage.Internal
{
    public class FbRelationalTransaction : RelationalTransaction
    {
        private readonly IRelationalConnection _relationalConnection;
        private readonly DbTransaction _dbTransaction;
        private readonly IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> _logger;
        private readonly bool _transactionOwned;

        private bool _connectionClosed;

        public FbRelationalTransaction(
            [NotNull] IRelationalConnection connection,
            [NotNull] DbTransaction transaction,
            [NotNull] IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger,
            bool transactionOwned)
            : base(connection, transaction, logger, transactionOwned)
        {
            if (connection.DbConnection != transaction.Connection)
            {
                throw new InvalidOperationException(RelationalStrings.TransactionAssociatedWithDifferentConnection);
            }

            _relationalConnection = connection;
            _dbTransaction = transaction;
            _logger = logger;
            _transactionOwned = transactionOwned;
        }

        public virtual async Task CommitAsync(CancellationToken cancellationToken=default(CancellationToken))
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                (_dbTransaction as FbTransaction).Commit();

                _logger.TransactionCommitted(
                    _relationalConnection,
                    _dbTransaction,
                    TransactionId,
                    startTime,
                    stopwatch.Elapsed);
            }
            catch (Exception e)
            {
                _logger.TransactionError(
                    _relationalConnection,
                    _dbTransaction,
                    TransactionId,
                    "CommitAsync",
                    e,
                    startTime,
                    stopwatch.Elapsed);
                throw;
            }

            ClearTransaction();
        }

        /// <summary>
        ///     Discards all changes made to the database in the current transaction.
        /// </summary>
        public virtual async Task RollbackAsync(CancellationToken cancellationToken=default(CancellationToken))
        {
            var startTime = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                (_dbTransaction as FbTransaction).Rollback();

                _logger.TransactionRolledBack(
                    _relationalConnection,
                    _dbTransaction,
                    TransactionId,
                    startTime,
                    stopwatch.Elapsed);
            }
            catch (Exception e)
            {
                _logger.TransactionError(
                    _relationalConnection,
                    _dbTransaction,
                    TransactionId,
                    "RollbackAsync",
                    e,
                    startTime,
                    stopwatch.Elapsed);
                throw;
            }

            ClearTransaction();
        }

        private void ClearTransaction()
        {
            Debug.Assert(_relationalConnection.CurrentTransaction == null || _relationalConnection.CurrentTransaction == this);

            _relationalConnection.UseTransaction(null);

            if (!_connectionClosed)
            {
                _connectionClosed = true;

                _relationalConnection.Close();
            }
        }

    }
}
