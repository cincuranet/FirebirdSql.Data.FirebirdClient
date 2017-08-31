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
        public static FbPropertyAnnotations Fb([NotNull] this IMutableProperty property)
            => (FbPropertyAnnotations)Fb((IProperty)property);
        
        public static IFbPropertyAnnotations Fb([NotNull] this IProperty property)
            => new FbPropertyAnnotations(Check.NotNull(property, nameof(property)));
        
        public static FbEntityTypeAnnotations Fb([NotNull] this IMutableEntityType entityType)
            => (FbEntityTypeAnnotations)Fb((IEntityType)entityType);
        
        public static IFbEntityTypeAnnotations Fb([NotNull] this IEntityType entityType)
            => new FbEntityTypeAnnotations(Check.NotNull(entityType, nameof(entityType)));
        
        public static FbKeyAnnotations Fb([NotNull] this IMutableKey key)
            => (FbKeyAnnotations)Fb((IKey)key);
        
        public static IFbKeyAnnotations Fb([NotNull] this IKey key)
            => new FbKeyAnnotations(Check.NotNull(key, nameof(key)));
        
        public static FbIndexAnnotations Fb([NotNull] this IMutableIndex index)
            => (FbIndexAnnotations)Fb((IIndex)index);
        
        public static IFbIndexAnnotations Fb([NotNull] this IIndex index)
            => new FbIndexAnnotations(Check.NotNull(index, nameof(index)));
        
        public static FbModelAnnotations Fb([NotNull] this IMutableModel model)
            => (FbModelAnnotations)Fb((IModel)model);
        
        public static IFbModelAnnotations Fb([NotNull] this IModel model)
            => new FbModelAnnotations(Check.NotNull(model, nameof(model)));
    }
}
