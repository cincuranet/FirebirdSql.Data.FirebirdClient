﻿/*
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

//$Authors = Carlos Guzman Alvarez, Jiri Cincura (jiri@cincura.net)

using System;
using System.Transactions;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.TestsBase;
using NUnit.Framework;

namespace FirebirdSql.Data.FirebirdClient.Tests
{
	[FbTestFixture(FbServerType.Default, false)]
	[FbTestFixture(FbServerType.Default, true)]
	[FbTestFixture(FbServerType.Embedded, default(bool))]
	public class TransactionScopeTests : FbTestsBase
	{
		#region Constructors

		public TransactionScopeTests(FbServerType serverType, bool compression)
			: base(serverType, compression)
		{ }

		#endregion

		#region Unit Tests

		[Test]
		public void SimpleSelectTest()
		{
			FbConnectionStringBuilder csb = BuildConnectionStringBuilder(FbServerType, Compression);

			csb.Enlist = true;

			using (TransactionScope scope = new TransactionScope())
			{
				using (FbConnection c = new FbConnection(csb.ToString()))
				{
					c.Open();

					using (FbCommand command = new FbCommand("select * from TEST where (0=1)", c))
					{
						using (FbDataReader r = command.ExecuteReader())
						{
							while (r.Read())
							{
							}
						}
					}
				}

				scope.Complete();
			}
		}

		[Test]
		public void InsertTest()
		{
			FbConnectionStringBuilder csb = BuildConnectionStringBuilder(FbServerType, Compression);

			csb.Enlist = true;

			using (TransactionScope scope = new TransactionScope())
			{
				using (FbConnection c = new FbConnection(csb.ToString()))
				{
					c.Open();

					string sql = "insert into TEST (int_field, date_field) values (1002, @date)";

					using (FbCommand command = new FbCommand(sql, c))
					{
						command.Parameters.Add("@date", FbDbType.Date).Value = DateTime.Now.ToString();

						int ra = command.ExecuteNonQuery();

						Assert.AreEqual(ra, 1);
					}
				}

				scope.Complete();
			}
		}

		#endregion
	}
}

