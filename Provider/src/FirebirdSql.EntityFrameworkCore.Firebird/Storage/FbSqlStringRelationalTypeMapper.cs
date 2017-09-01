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

using FirebirdSql.Data.FirebirdClient;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal; 
using System.Collections.Concurrent; 

namespace FirebirdSql.EntityFrameworkCore.Firebird.Storage
{
    public class FbStringRelationalTypeMapper : IStringRelationalTypeMapper
    {

        static readonly RelationalTypeMapping UnboundedStringMapping
               = new FbTypeMapping("BLOB SUB_TYPE TEXT", typeof(string), FbDbType.Text);

        readonly ConcurrentDictionary<int, RelationalTypeMapping> _boundedStringMappings
            = new ConcurrentDictionary<int, RelationalTypeMapping>();

        public RelationalTypeMapping FindMapping(bool unicode, bool keyOrIndex, int? maxLength)
        {
            return maxLength.HasValue
                ? _boundedStringMappings.GetOrAdd(maxLength.Value,
                      ml => new FbTypeMapping($"VARCHAR({maxLength})", typeof(string))
                  )
                : UnboundedStringMapping;
        }

    }
}


