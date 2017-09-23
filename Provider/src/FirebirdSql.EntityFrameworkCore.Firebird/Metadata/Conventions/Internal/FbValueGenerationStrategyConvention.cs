/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/blob/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$Authors = Jiri Cincura (jiri@cincura.net), Rafael Almeida (ralms@ralms.net)

using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using FirebirdSql.EntityFrameworkCore.Firebird.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Metadata.Conventions.Internal
{ 
    public class FbValueGenerationStrategyConvention : DatabaseGeneratedAttributeConvention, IModelInitializedConvention
    {
        public override InternalPropertyBuilder Apply(InternalPropertyBuilder propertyBuilder, DatabaseGeneratedAttribute attribute, MemberInfo clrMember)
        {
            FbValueGenerationStrategy? valueGenerationStrategy = null;
            ValueGenerated valueGenerated = ValueGenerated.Never;
            if (attribute.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
            {
                valueGenerated = ValueGenerated.OnAdd;
                valueGenerationStrategy = FbValueGenerationStrategy.IdentityColumn;
            }

            propertyBuilder.ValueGenerated(valueGenerated, ConfigurationSource.Convention);
            propertyBuilder.Firebird(ConfigurationSource.DataAnnotation).ValueGenerationStrategy(valueGenerationStrategy); 
            return base.Apply(propertyBuilder, attribute, clrMember);
        }
		 
        public virtual InternalModelBuilder Apply(InternalModelBuilder modelBuilder)
        {
            modelBuilder.Firebird(ConfigurationSource.Convention).ValueGenerationStrategy(FbValueGenerationStrategy.IdentityColumn); 
            return modelBuilder;
        }
    }
}
