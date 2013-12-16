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
 *  All Rights Reserved.
  *   
 *  Contributors:
 *    Luis Madaleno (madaleno@magnisoft.com)
*/

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Entity;
using NUnit.Framework;

namespace FirebirdSql.Data.UnitTests
{
    [TestFixture]
    class NamingStrategyTests : EntityFrameworkTests
    {
		#region · Constructors ·

        public NamingStrategyTests()
        { }

		#endregion

        #region · Unit Tests ·
        [Test]
        public void TestDefaultNamingStrategy()
        {
            Database.SetInitializer<QueryTest1Context>(null);
            Connection.Close();
            using (var c = new QueryTest2Context(Connection))
            {
                var q = c.Foos
                    .OrderBy(x => x.ID)
                    .Take(45).Skip(0)
                    .Select(x => new
                    {
                        x.ID,
                        x.BazID,
                        BazID2 = x.Baz.ID,
                        x.Baz.BazString,
                    });
                Assert.DoesNotThrow(() =>
                {
                    Console.WriteLine(q.ToString());
                });
            }
        }
    
        [Test]
        public void TestUpperNoQuotesNamingStrategy()
        {
            SqlGenerator.NamingStrategy = new NamingStrategyUpperNoQuotes();

            Database.SetInitializer<QueryTest1Context>(null);
            Connection.Close();
            using (var c = new QueryTest2Context(Connection))
            {
                var q = c.Foos
                    .OrderBy(x => x.ID)
                    .Take(45).Skip(0)
                    .Select(x => new
                    {
                        x.ID,
                        x.BazID,
                        BazID2 = x.Baz.ID,
                        x.Baz.BazString,
                    });
                Assert.DoesNotThrow(() =>
                {
                    Console.WriteLine(q.ToString());
                });
            }
        }

        #endregion
    }
}
