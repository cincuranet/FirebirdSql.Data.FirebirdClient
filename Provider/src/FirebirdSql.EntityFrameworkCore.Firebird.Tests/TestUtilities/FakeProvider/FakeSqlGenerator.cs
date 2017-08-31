﻿/// Copyright (c) Pomelo Foundation. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Update;
using System.Text;

namespace Microsoft.EntityFrameworkCore.TestUtilities.FakeProvider
{
    public class FakeSqlGenerator : UpdateSqlGenerator
    {
        public FakeSqlGenerator(UpdateSqlGeneratorDependencies dependencies)
            : base(dependencies)
        {
        }

        public override ResultSetMapping AppendInsertOperation(StringBuilder commandStringBuilder, ModificationCommand command, int commandPosition)
        {
            if (!string.IsNullOrEmpty(command.Schema))
            {
                commandStringBuilder.Append(command.Schema + ".");
            }
            commandStringBuilder.Append(command.TableName);

            return ResultSetMapping.NotLastInResultSet;
        }

        public int AppendBatchHeaderCalls { get; set; }

        public override void AppendBatchHeader(StringBuilder commandStringBuilder)
        {
            AppendBatchHeaderCalls++;
            base.AppendBatchHeader(commandStringBuilder);
        }

        protected override ResultSetMapping AppendSelectAffectedCountCommand(
            StringBuilder commandStringBuilder, string name, string schema, int commandPosition)
        {
            return ResultSetMapping.NoResultSet;
        }

        protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
        {
        }

        protected override void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification)
        {
        }
    }
}
