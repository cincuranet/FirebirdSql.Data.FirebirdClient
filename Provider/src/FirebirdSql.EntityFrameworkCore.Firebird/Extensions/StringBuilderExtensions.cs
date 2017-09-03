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

using System.Collections.Generic;

namespace System.Text
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendJoin(
                this StringBuilder stringBuilder, IEnumerable<string> values, string separator = ", ")
                => stringBuilder.AppendJoin(values, (sb, value) => sb.Append(value), separator);

        public static StringBuilder AppendJoin(
            this StringBuilder stringBuilder, string separator, params string[] values)
            => stringBuilder.AppendJoin(values, (sb, value) => sb.Append(value), separator);

        public static StringBuilder AppendJoin<T>(
            this StringBuilder stringBuilder,
            IEnumerable<T> values,
            Action<StringBuilder, T> joinAction,
            string separator = ", ")
        {
            var appended = false;

            foreach (var value in values)
            {
                joinAction(stringBuilder, value);
                stringBuilder.Append(separator);
                appended = true;
            }

            if (appended) 
                stringBuilder.Length -= separator.Length; 

            return stringBuilder;
        }

        public static StringBuilder AppendJoin<T, TParam>(
            this StringBuilder stringBuilder,
            IEnumerable<T> values,
            TParam param,
            Action<StringBuilder, T, TParam> joinAction,
            string separator = ", ")
        {
            var appended = false; 
            foreach (var value in values)
            {
                joinAction(stringBuilder,  value, param);
                stringBuilder.Append(separator);
                appended = true;
            }

            if (appended) 
                stringBuilder.Length -= separator.Length;
         
            return stringBuilder;
        }

        public static StringBuilder AppendJoinUpadate<T, TParam>(
            this StringBuilder stringBuilder,
            IEnumerable<T> values,
            TParam param,
            Action<StringBuilder, T, TParam> joinAction,
            string separator = ", ")
        {
            var appended = false;

            foreach (var value in values)
            {
                joinAction(stringBuilder,value, param);
                stringBuilder.Append(separator);
                appended = true;
            }

            if (appended)
                stringBuilder.Length -= separator.Length;

            return stringBuilder;
        }

        public static StringBuilder AppendJoin<T, TParam1, TParam2>(
            this StringBuilder stringBuilder,
            IEnumerable<T> values,
            TParam1 param1,
            TParam2 param2,
            Action<StringBuilder, T, TParam1, TParam2> joinAction,
            string separator = ", ")
        {
            var appended = false;

            foreach (var value in values)
            {
                joinAction(stringBuilder, value, param1, param2);
                stringBuilder.Append(separator);
                appended = true;
            }

            if (appended) 
                stringBuilder.Length -= separator.Length;
            

            return stringBuilder;
        }
    }
}
