// Copyright (c) Pomelo Foundation. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.TestUtilities.FakeProvider
{
    public class FakeRelationalConnection : RelationalConnection
    {
        private readonly List<FakeDbConnection> _dbConnections = new List<FakeDbConnection>();

        public FakeRelationalConnection(IDbContextOptions options)
            : base(
                new RelationalConnectionDependencies(
                    options,
                    new DiagnosticsLogger<DbLoggerCategory.Database.Transaction>(
                        new LoggerFactory(),
                        new LoggingOptions(),
                        new DiagnosticListener("FakeDiagnosticListener")),
                    new DiagnosticsLogger<DbLoggerCategory.Database.Connection>(
                        new LoggerFactory(),
                        new LoggingOptions(),
                        new DiagnosticListener("FakeDiagnosticListener")),
                    new NamedConnectionStringResolver(options)))
        {
        }

        public IReadOnlyList<FakeDbConnection> DbConnections => _dbConnections;

        protected override DbConnection CreateDbConnection()
        {
            var connection = new FakeDbConnection(ConnectionString);

            _dbConnections.Add(connection);

            return connection;
        }
    }
}
