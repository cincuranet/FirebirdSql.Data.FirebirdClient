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

namespace Microsoft.EntityFrameworkCore.Scaffolding.Internal
{
    internal static class FbTableSelectionSetExtensions
    {
        public static bool Allows(this TableSelectionSet _tableSelectionSet, /* [NotNull] */ string schemaName, /* [NotNull] */ string tableName)
        {
            if (_tableSelectionSet == null
                || (_tableSelectionSet.Schemas.Count == 0
                && _tableSelectionSet.Tables.Count == 0))
            {
                return true;
            }

            var result = false;

            foreach (var schemaSelection in _tableSelectionSet.Schemas)
                if (EqualsWithQuotes(schemaSelection.Text, schemaName))
                {
                    schemaSelection.IsMatched = true;
                    result = true;
                }

            foreach (var tableSelection in _tableSelectionSet.Tables)
            {
                var components = tableSelection.Text.Split('.');
                if (components.Length == 1
                    ? EqualsWithQuotes(components[0], tableName)
                    : EqualsWithQuotes(components[0], schemaName) && EqualsWithQuotes(components[1], tableName))
                {
                    tableSelection.IsMatched = true;
                    result = true;
                }
            }

            return result;
        }

        static bool EqualsWithQuotes(string expr, string name) =>
            expr[0] == '"' && expr[expr.Length - 1] == '"'
                ? expr.Substring(0, expr.Length - 2).Equals(name)
                : expr.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
