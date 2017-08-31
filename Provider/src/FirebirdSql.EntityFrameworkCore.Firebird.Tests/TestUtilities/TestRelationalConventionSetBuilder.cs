// Copyright (c) Pomelo Foundation. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;

namespace Microsoft.EntityFrameworkCore.TestUtilities
{
    public class TestRelationalConventionSetBuilder : RelationalConventionSetBuilder
    {
        public TestRelationalConventionSetBuilder(RelationalConventionSetBuilderDependencies dependencies)
            : base(dependencies)
        {
        }

        public static ConventionSet Build()
        {
            var sqlServerTypeMapper = new FbTypeMapper(new RelationalTypeMapperDependencies());
            return new TestRelationalConventionSetBuilder(
                    new RelationalConventionSetBuilderDependencies(
                        new TestRelationalTypeMapper(new RelationalTypeMapperDependencies()), null, null))
                .AddConventions(
                    new CoreConventionSetBuilder(new CoreConventionSetBuilderDependencies(sqlServerTypeMapper))
                        .CreateConventionSet());
        }
    }

}
