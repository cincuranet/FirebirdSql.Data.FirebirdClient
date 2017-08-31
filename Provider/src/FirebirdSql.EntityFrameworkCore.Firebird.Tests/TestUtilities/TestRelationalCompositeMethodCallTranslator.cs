// Copyright (c) Pomelo Foundation. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Tests.TestUtilities
{
    public class TestRelationalCompositeMethodCallTranslator : RelationalCompositeMethodCallTranslator
    {
        public TestRelationalCompositeMethodCallTranslator(RelationalCompositeMethodCallTranslatorDependencies dependencies)
            : base(dependencies)
        {
        }
    }
}
