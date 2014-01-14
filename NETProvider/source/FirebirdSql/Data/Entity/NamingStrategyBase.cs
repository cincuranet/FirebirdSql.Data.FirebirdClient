/*
 *  Firebird ADO.NET Data provider for .NET and Mono 
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
 *  Copyright (c) 2008-2013 Jiri Cincura (jiri@cincura.net)
 *  Copyright (c) 2013      Luis Madaleno (madaleno@magnisoft.com)
 *  All Rights Reserved.
 */

using System;

namespace FirebirdSql.Data.Entity
{
    public class NamingStrategyBase: INamingStrategy
    {
        /// <summary>
        /// Decorate with double quotes and escape double quotes inside in Firebird.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual string QuoteIdentifier(string name)
        {
            return "\"" + name.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// Decorates a table name acording to the implemented stategy
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetTableName(string name)
        {
            return QuoteIdentifier(name);
        }

        /// <summary>
        /// Decorates a column name acording to the implemented stategy
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetColumnName(string name)
        {
            return QuoteIdentifier(name);
        }
    }
}
