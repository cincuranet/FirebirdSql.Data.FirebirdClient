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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace Microsoft.EntityFrameworkCore.Update.Internal
{

    public class FbBatchExecutor : IBatchExecutor
    { 
        public int Execute(IEnumerable<ModificationCommandBatch> commandBatches,
            IRelationalConnection connection)
        {
            int recordAffecteds = 0;
            if(connection?.DbConnection?.State != System.Data.ConnectionState.Open)
                connection.Open();

            IDbContextTransaction currentTransaction = null;
            try
            {
                if (connection.CurrentTransaction == null) 
                    currentTransaction = connection.BeginTransaction(); 

                foreach (var commandbatch in commandBatches)
                {
                    commandbatch.Execute(connection);
	                recordAffecteds += commandbatch.ModificationCommands.Count;
                } 
                currentTransaction?.Commit();
                currentTransaction?.Dispose();
            }
            catch(Exception ex)
            { 
                try
                {
                    currentTransaction?.Rollback();
                    currentTransaction?.Dispose();
                }
                catch{}
                throw ex;
            }
            finally
            {
                connection?.Close();
            } 
            return recordAffecteds;
        }

        public async Task<int> ExecuteAsync(
            IEnumerable<ModificationCommandBatch> commandBatches,
            IRelationalConnection connection,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var RowsAffecteds = 0;
            await connection.OpenAsync(cancellationToken, false).ConfigureAwait(false);
            FbRelationalTransaction currentTransaction = null;
            try
            {
                if (connection.CurrentTransaction == null) 
                    currentTransaction = await (connection as FbRelationalConnection).BeginTransactionAsync(cancellationToken).ConfigureAwait(false) as FbRelationalTransaction;
               

                foreach (var commandbatch in commandBatches)
                {
                    await commandbatch.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);
                    RowsAffecteds += commandbatch.ModificationCommands.Count;
                }

                if (currentTransaction != null) 
                    await currentTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
           
                currentTransaction?.Dispose();
            }
            catch (Exception err)
            {
                try
                {
                    currentTransaction?.Rollback();
                    currentTransaction?.Dispose();
                }
                catch{}
                throw err;
            }
            finally
            {
                connection?.Close();
            } 
            return RowsAffecteds;
        }
    }
}
