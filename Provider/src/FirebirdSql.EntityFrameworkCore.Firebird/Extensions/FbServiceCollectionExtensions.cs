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


using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;


namespace Microsoft.Extensions.DependencyInjection
{
    public static class FbServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkFb([NotNull] this IServiceCollection serviceCollection)
        {
            Check.NotNull(serviceCollection, nameof(serviceCollection));

            var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
                 .TryAdd<IRelationalDatabaseCreator, FbDatabaseCreator>()
                 .TryAdd<IDatabaseProvider, DatabaseProvider<FbOptionsExtension>>()
                 .TryAdd<IRelationalTypeMapper, FbTypeMapper>()
                .TryAdd<IRelationalCommandBuilderFactory, FbCommandBuilderFactory>()
                .TryAdd<ISqlGenerationHelper, FbSqlGenerationHelper>()
                .TryAdd<IMigrationsAnnotationProvider, FbMigrationsAnnotationProvider>()
                .TryAdd<IConventionSetBuilder, FbConventionSetBuilder>()
                .TryAdd<IUpdateSqlGenerator>(p => p.GetService<IFbUpdateSqlGenerator>())
                .TryAdd<IModificationCommandBatchFactory, FbModificationCommandBatchFactory>()
                .TryAdd<IValueGeneratorSelector, FbValueGeneratorSelector>()
                .TryAdd<IRelationalConnection>(p => p.GetService<IFbRelationalConnection>())

                .TryAdd<IMigrationsSqlGenerator, FbMigrationsSqlGenerator>()
               .TryAdd<IBatchExecutor, FbBatchExecutor>()
                .TryAdd<IBatchExecutor, BatchExecutor>()

                .TryAdd<IHistoryRepository, FbHistoryRepository>()
                .TryAdd<IMemberTranslator, FbCompositeMemberTranslator>()
                .TryAdd<ICompositeMethodCallTranslator, FbCompositeMethodCallTranslator>()
                .TryAdd<IQuerySqlGeneratorFactory, FbQuerySqlGeneratorFactory>()
                .TryAdd<ISingletonOptions, IFbOptions>(p => p.GetService<IFbOptions>())
                .TryAddProviderSpecificServices(b => b
                    .TryAddSingleton<IFbOptions, FbOptions>()
                    .TryAddScoped<IFbUpdateSqlGenerator, FbUpdateSqlGenerator>()
                    .TryAddScoped<IFbRelationalConnection, FbRelationalConnection>());

            builder.TryAddCoreServices();

            return serviceCollection;
        }
    }
}
