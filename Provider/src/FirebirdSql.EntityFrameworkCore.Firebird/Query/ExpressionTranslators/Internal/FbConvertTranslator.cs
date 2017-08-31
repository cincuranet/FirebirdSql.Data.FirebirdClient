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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query.Expressions;

namespace Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class FbConvertTranslator : IMethodCallTranslator
    {
        private static readonly Dictionary<string, string> _typeMapping = new Dictionary<string, string>
        {
            [nameof(Convert.ToByte)] = "SMALLINT",
            [nameof(Convert.ToDecimal)] = "DECIMAL(18,4)",
            [nameof(Convert.ToDouble)] = "DOUBLE PRECISION",
            [nameof(Convert.ToDouble)] = "FLOAT",
            [nameof(Convert.ToInt16)] = "SMALLINT",
            [nameof(Convert.ToInt32)] = "INTEGER",
            [nameof(Convert.ToInt64)] = "BIGINT",
            [nameof(Convert.ToString)] = "VARCHAR(4000)"
        };

        private static readonly List<Type> _supportedTypes = new List<Type>
        {
            typeof(bool),
            typeof(byte),
            typeof(decimal),
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(long),
            typeof(short),
            typeof(string)
        };

        private static readonly IEnumerable<MethodInfo> _supportedMethods
            = _typeMapping.Keys
                .SelectMany(t => typeof(Convert).GetTypeInfo().GetDeclaredMethods(t)
                    .Where(m => m.GetParameters().Length == 1
                                && _supportedTypes.Contains(m.GetParameters().First().ParameterType)));

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual Expression Translate(MethodCallExpression methodCallExpression)
            => _supportedMethods.Contains(methodCallExpression.Method)
                ? new ExplicitCastExpression(methodCallExpression.Arguments[0], methodCallExpression.Type)
                : null;

       
    }
}
