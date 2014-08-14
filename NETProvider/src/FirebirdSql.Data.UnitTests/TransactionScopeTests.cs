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
using System.Data;
using System.Threading;
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
			var tso = new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted };
			using (var scope = new TransactionScope(TransactionScopeOption.Required, tso))
			{
				using (var connection = new FbConnection(csb.ToString()))
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
			var tso = new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted };
			using (var scope = new TransactionScope(TransactionScopeOption.Required, tso))
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
		public void Commit_SingleConnection()
		{
			FbConnectionStringBuilder csb = BuildConnectionStringBuilder();
			csb.Enlist = true;

			using (TransactionScope scope = new TransactionScope())
			{
				using (var connection = new FbConnection(csb.ToString()))
				{
					connection.Open();

					using (FbCommand command = new FbCommand("insert into TEST (int_field, date_field) values (1002, @date)", connection))
					{
						command.Parameters.Add("@date", FbDbType.Date).Value = DateTime.Now.ToString();

						var result = command.ExecuteNonQuery();

						Assert.AreEqual(result, 1);
					}
				}

				scope.Complete();
			}
		}

		[Test]
		public void Rollback_SingleConnection()
		{
			var csb = BuildConnectionStringBuilder();
			csb.Enlist = true;
			var connectionString = csb.ToString();
			var tso = new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted };
			using (var scope = new TransactionScope(TransactionScopeOption.Required, tso))
			{
				using (var connection = new FbConnection(connectionString))
				{
					connection.Open();
					Assert.That(ExecuteNonQuery("insert into TEST (int_field) values (1002)", connection), Is.EqualTo(1));
				}
				// DO NOT COMMIT
			}
			Assert.That(ExecuteScalar("select count(*) from test where int_field = 1002"), Is.EqualTo(0));
		}

		[Test]
		public void Rollback_MultipleConnections_ShouldPromoteToDTC()
		{
			var csb = BuildConnectionStringBuilder();
			csb.Enlist = true;
			var connectionString = csb.ToString();

			var tso = new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted };
			using (var scope = new TransactionScope(TransactionScopeOption.Required, tso))
			{
				// use one connection
				using (var connection = new FbConnection(connectionString))
				{
					connection.Open();
					var command = new FbCommand("insert into test (int_field, varchar_field) values (@p0, @p1)", connection);
					command.Parameters.Add(new FbParameter("p0", 2014));
					command.Parameters.Add(new FbParameter("p1", "test1"));
					object result = command.ExecuteNonQuery();
					Assert.That(result, Is.EqualTo(1));

					var command2 = new FbCommand("select varchar_field from test where varchar_field = 'test1'", connection);
					result = command2.ExecuteScalar();
					Assert.That(result, Is.EqualTo("test1"));
				}

				// use another connection
				using (var connection = new FbConnection(connectionString))
				{
					connection.Open();
					var command = new FbCommand("insert into test (int_field, varchar_field) values (@p0, @p1)", connection);
					command.Parameters.Add(new FbParameter("p0", 2015));
					command.Parameters.Add(new FbParameter("p1", "test2"));
					object result = command.ExecuteNonQuery();
					Assert.That(result, Is.EqualTo(1));

					var command2 = new FbCommand("select int_field from test where int_field = 2015", connection);
					result = command2.ExecuteScalar();
					Assert.That(result, Is.EqualTo(2015));

					// make sure we can't see the test1 value since we're using a new connection
					var command3 = new FbCommand("select varchar_field from test where varchar_field = @p0", connection);
					command3.Parameters.Add(new FbParameter("p0", "test1"));
					result = command3.ExecuteScalar();
					Assert.That(result, Is.Null);
				}
				// DO NOT COMMIT
			}

			// Pause a little so that DTC can do its Job
			System.Threading.Thread.Sleep(500);

			using (var connection = new FbConnection(connectionString))
			{
				connection.Open();
				var command1 = new FbCommand("select varchar_field from test where varchar_field = 'test'", connection);
				var result = command1.ExecuteScalar();
				Assert.That(result, Is.Null);

				var command2 = new FbCommand("select int_field from test where int_field = 2014", connection);
				result = command2.ExecuteScalar();
				Assert.That(result, Is.Null);
			}
		}

		[Test]
		public void Commit_MultipleConnections_ShouldPromoteToDTC()
		{
			var csb = BuildConnectionStringBuilder();
			csb.Enlist = true;
			var connectionString = csb.ToString();

			var tso = new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted };
			using (var scope = new TransactionScope(TransactionScopeOption.Required, tso))
			{
				// first connection
				using (var connection = new FbConnection(connectionString))
				{
					connection.Open();
					var command = new FbCommand("insert into test (int_field, varchar_field) values (@p0, @p1)", connection);
					command.Parameters.Add(new FbParameter("p0", 2014));
					command.Parameters.Add(new FbParameter("p1", "test1"));
					object result = command.ExecuteNonQuery();
					Assert.That(result, Is.EqualTo(1));

					command = new FbCommand("select int_field from test where varchar_field = 'test1'", connection);
					result = command.ExecuteScalar();
					Assert.That(result, Is.EqualTo(2014));
				}

				// second connection
				using (var connection = new FbConnection(connectionString))
				{
					connection.Open();
					var command = new FbCommand("insert into test (int_field, varchar_field) values (@p0, @p1)", connection);
					command.Parameters.Add(new FbParameter("p0", 2015));
					command.Parameters.Add(new FbParameter("p1", "test2"));
					object result = command.ExecuteNonQuery();
					Assert.That(result, Is.EqualTo(1));

					command = new FbCommand("select int_field from test where varchar_field = 'test2'", connection);
					result = command.ExecuteScalar();
					Assert.That(result, Is.EqualTo(2015));
				}
				// COMMIT
				scope.Complete();
			}

			// Pause a little so that MSDTC can do its Job
			System.Threading.Thread.Sleep(500);

			using (var connection = new FbConnection(connectionString))
			{
				connection.Open();
				var command = new FbCommand("select int_field from test where varchar_field = 'test1'", connection);
				var result = command.ExecuteScalar();
				Assert.That(result, Is.EqualTo(2014));

				command = new FbCommand("select int_field from test where varchar_field = 'test2'", connection);
				result = command.ExecuteScalar();
				Assert.That(result, Is.EqualTo(2015));
			}
		}

		[Test]
		public void Commit_OneTransactionParticipatorFails_ShouldRollbackAllChanges()
		{
			var csb = BuildConnectionStringBuilder();
			csb.Enlist = true;
			var connectionString = csb.ToString();

			var tso = new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted };
			try
			{
				using (var scope = new TransactionScope(TransactionScopeOption.Required, tso))
				{
					using (var connection = new FbConnection(connectionString))
					{
						connection.Open();
						var command = new FbCommand("insert into test (int_field, varchar_field) values (@p0, @p1)", connection);
						command.Parameters.Add(new FbParameter("p0", 2014));
						command.Parameters.Add(new FbParameter("p1", "test1"));
						object result = command.ExecuteNonQuery();
						Assert.That(result, Is.EqualTo(1));

						command = new FbCommand("select int_field from test where varchar_field = 'test1'", connection);
						result = command.ExecuteScalar();
						Assert.That(result, Is.EqualTo(2014));
					}

					// Force the transaction to rollback
					new ForceEscalationToDistributedTx(true);

					// COMMIT
					scope.Complete();
				}
				Assert.Fail("Expected tx abort");
			}
			catch (TransactionAbortedException)
			{
				using (var connection = new FbConnection(connectionString))
				{
					connection.Open();
					var command = new FbCommand("select int_field from test where varchar_field = 'test1'", connection);
					var result = command.ExecuteScalar();
					Assert.That(result, Is.Null);
				}
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

		public class ForceEscalationToDistributedTx : IEnlistmentNotification
		{
			private readonly bool _shouldRollback;
			private readonly int _thread;

			public ForceEscalationToDistributedTx(bool shouldRollBack)
			{
				_shouldRollback = shouldRollBack;
				_thread = Thread.CurrentThread.ManagedThreadId;
				System.Transactions.Transaction.Current.EnlistDurable(Guid.NewGuid(), this, EnlistmentOptions.None);
			}

			public ForceEscalationToDistributedTx()
				: this(false)
			{
			}

			public void Prepare(PreparingEnlistment preparingEnlistment)
			{
				Assert.AreNotEqual(_thread, Thread.CurrentThread.ManagedThreadId);

				if (_shouldRollback)
					preparingEnlistment.ForceRollback();
				else
					preparingEnlistment.Prepared();
			}

			public void Commit(Enlistment enlistment)
			{
				enlistment.Done();
			}

			public void Rollback(Enlistment enlistment)
			{
				enlistment.Done();
			}

			public void InDoubt(Enlistment enlistment)
			{
				enlistment.Done();
			}
		}
	}
}

