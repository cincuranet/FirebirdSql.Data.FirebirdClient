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
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore
{

    public static class FbMetadataExtensions
    {
        public static FbPropertyAnnotations Firebird([NotNull] this IMutableProperty property)
            => (FbPropertyAnnotations)Firebird((IProperty)property);
        
        public static IFbPropertyAnnotations Firebird([NotNull] this IProperty property)
            => new FbPropertyAnnotations(Check.NotNull(property, nameof(property)));
        
        public static FbEntityTypeAnnotations Firebird([NotNull] this IMutableEntityType entityType)
            => (FbEntityTypeAnnotations)Firebird((IEntityType)entityType);
        
        public static IFbEntityTypeAnnotations Firebird([NotNull] this IEntityType entityType)
            => new FbEntityTypeAnnotations(Check.NotNull(entityType, nameof(entityType)));
        
        public static FbKeyAnnotations Firebird([NotNull] this IMutableKey key)
            => (FbKeyAnnotations)Firebird((IKey)key);
        
        public static IFbKeyAnnotations Firebird([NotNull] this IKey key)
            => new FbKeyAnnotations(Check.NotNull(key, nameof(key)));
        
        public static FbModelAnnotations Firebird([NotNull] this IMutableModel model)
            => (FbModelAnnotations)Firebird((IModel)model);
        
        public static IFbModelAnnotations Firebird([NotNull] this IModel model)
            => new FbModelAnnotations(Check.NotNull(model, nameof(model)));
    }
}
