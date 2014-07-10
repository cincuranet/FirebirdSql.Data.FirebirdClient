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
 *  Copyright (c) 2006 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

using System;
using System.Transactions;

using FirebirdSql.Data.FirebirdClient;
using NUnit.Framework;

namespace FirebirdSql.Data.UnitTests
{
	/// <summary>
	/// All the test in this TestFixture are using implicit transaction support.
	/// </summary>
	[TestFixture]
	public class TransactionScopeTests : TestsBase
	{
		[Test]
		public void ExplicitEnlist()
		{
			var csb = BuildConnectionStringBuilder();
			using (var connection = new FbConnection(csb.ToString()))
			{
				using (var scope = new TransactionScope())
				{
					connection.Open();
					connection.EnlistTransaction(System.Transactions.Transaction.Current);
					Assert.That(ExecuteNonQuery("insert into TEST (int_field) values (1002)", connection), Is.EqualTo(1));
					scope.Complete();
				}
			}
			Assert.That(ExecuteScalar("select count(*) from test where int_field = 1002"), Is.EqualTo(1));
		}

		[Test]
		public void ImplicitEnlist()
		{
			var csb = BuildConnectionStringBuilder();
			csb.Enlist = true;
			var connectionString = csb.ToString();
			using (var scope = new TransactionScope())
			{
				using (var connection = new FbConnection(connectionString))
				{
					connection.Open();
					Assert.That(ExecuteNonQuery("insert into TEST (int_field) values (1002)", connection), Is.EqualTo(1));
				}
				scope.Complete();
			}
			Assert.That(ExecuteScalar("select count(*) from test where int_field = 1002"), Is.EqualTo(1));
		}

		[Test]
		public void ImplicitEnlist_WithoutTransactionScope_ShouldNotThrow()
		{
			var csb = BuildConnectionStringBuilder();
			csb.Enlist = true;
			var connectionString = csb.ToString();
			using (var connection = new FbConnection(connectionString))
			{
				connection.Open();
				Assert.That(ExecuteNonQuery("insert into TEST (int_field) values (1002)", connection), Is.EqualTo(1));
			}
			Assert.That(ExecuteScalar("select count(*) from test where int_field = 1002"), Is.EqualTo(1));
		}

		[Test]
		public void Rollback_SingleConnection()
		{
			var csb = BuildConnectionStringBuilder();
			csb.Enlist = true;
			var connectionString = csb.ToString();
			using (var scope = new TransactionScope())
			{
				using (var connection = new FbConnection(connectionString))
				{
					connection.Open();
					Assert.That(ExecuteNonQuery("insert into TEST (int_field) values (1002)", connection), Is.EqualTo(1));
					// DO NOT COMMIT
				}
			}
			Assert.That(ExecuteScalar("select count(*) from test where int_field = 1002"), Is.EqualTo(0));
		}

		[Test]
		public void SimpleSelectTest()
		{
			FbConnectionStringBuilder csb = BuildConnectionStringBuilder();

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
			FbConnectionStringBuilder csb = BuildConnectionStringBuilder();

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

		private int ExecuteNonQuery(string sql, FbConnection connection)
		{
			if (connection == null)
				connection = Connection;
			using (var cmd = new FbCommand(sql, connection))
			{
				return cmd.ExecuteNonQuery();
			}
		}

		private object ExecuteScalar(string sql, FbConnection connection = null)
		{
			if (connection == null)
				connection = Connection;
			using (var cmd = new FbCommand(sql, connection))
				return cmd.ExecuteScalar();
		}
	}
}

