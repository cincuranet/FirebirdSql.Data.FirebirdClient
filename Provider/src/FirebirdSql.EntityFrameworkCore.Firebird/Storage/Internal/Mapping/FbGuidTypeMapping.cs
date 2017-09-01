/*                 
 *            FirebirdSql.EntityFrameworkCore.Firebird
 *              https://www.firebirdsql.org/en/net-provider/ 
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
 *                  All Rights Reserved.
 */

using System;
using System.Data;
using System.Text;
using JetBrains.Annotations;
using FirebirdSql.Data.FirebirdClient;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore.Storage
{
    /// <summary>
    ///     <para>
    ///         Represents the mapping between a .NET <see cref="DateTime" /> type and a database type.
    ///     </para>
    ///     <para>
    ///         This type is typically used by database providers (and other extensions). It is generally
    ///         not used in application code.
    ///     </para>
    /// </summary>
    public class FirebirdGuidTypeMapping : GuidTypeMapping
    { 
        private readonly string _storeType;
        private readonly FbDbType _fbDbType;

        public FirebirdGuidTypeMapping(string storeType, FbDbType fbDbType)
            : base(storeType)
        {
            _fbDbType = fbDbType;
            _storeType = storeType;
        }

        protected override void ConfigureParameter([NotNull] DbParameter parameter)
            => ((FbParameter)parameter).FbDbType = _fbDbType;

        protected override string SqlLiteralFormatString => $"";
         
    }
}
