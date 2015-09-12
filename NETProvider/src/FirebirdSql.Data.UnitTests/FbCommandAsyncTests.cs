#if NET_45
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using System.Threading;

namespace FirebirdSql.Data.UnitTests
{
	[TestFixture(FbServerType.Default)]
	[TestFixture(FbServerType.Embedded)]
	public class FbCommandAsyncTests : TestsBase
	{
		public FbCommandAsyncTests(FbServerType serverType)
			: base(serverType)
		{ }

		[Test]
		public void TestExecuteScalarAsync()
		{
			string query = "SELECT FIRST(1) int_field from test";
			using (var command = this.Connection.CreateCommand())
			{
				command.CommandText = query;
				Task<object> ret = command.ExecuteScalarAsync();
				// If runs in another thread ret.Wait will return false. If call is not async it will be already executed and Wait returns true
				Assert.False(ret.Wait(0));
				ret.Wait();
			}
		}

		[Test]
		public void TestCancellationExecuteScalarAsync()
		{
			string query = "SELECT FIRST(1) int_field from test";
			using (var command = this.Connection.CreateCommand())
			{
				command.CommandText = query;
				CancellationTokenSource cts = new CancellationTokenSource();
				Task<object> ret = command.ExecuteScalarAsync(cts.Token);
				cts.Cancel();
				AssertOperationCancelledException(ret);
			}
		}

		[Test]
		public void TestExecuteNonQueryAsync()
		{
			string query = "SELECT FIRST(1) int_field from test";
			using (var command = this.Connection.CreateCommand())
			{
				command.CommandText = query;
				Task<int> ret = command.ExecuteNonQueryAsync();
				// If runs in another thread ret.Wait will return false. If call is not async it will be already executed and Wait returns true
				Assert.False(ret.Wait(0));
				ret.Wait();
			}
		}

		[Test]
		public void TestCancellationExecuteNonQueryAsync()
		{
			string query = "SELECT FIRST(1) int_field from test";
			using (var command = this.Connection.CreateCommand())
			{
				command.CommandText = query;
				CancellationTokenSource cts = new CancellationTokenSource();
				Task<int> ret = command.ExecuteNonQueryAsync(cts.Token);
				cts.Cancel();
				AssertOperationCancelledException(ret);
			}
		}

		private static void AssertOperationCancelledException(Task ret)
		{
			try
			{
				ret.Wait();
			}
			catch (AggregateException ex)
			{
				Assert.IsTrue(ex.InnerException is OperationCanceledException);
			}
		}

		[Test]
		public void TestExecuteReaderAsync()
		{
			using (FbTransaction transaction = Connection.BeginTransaction())
			{
				using (FbCommand command = new FbCommand("select * from TEST", Connection, transaction))
				{
					var readerOpenTask = command.ExecuteReaderAsync();
					Assert.False(readerOpenTask.Wait(0));
					using (var reader = readerOpenTask.Result )
					{
						var readTask = reader.ReadAsync();
						Assert.False(readTask.Wait(0));
						while (readTask.Result)
						{
							for (int i = 0; i < reader.FieldCount; i++)
							{
								Console.Write(reader.GetValue(i) + "\t");
							}							

							readTask = reader.ReadAsync();
							// do not check readTask time for Default server, because GdsStatement reads several records at once, and they already read.
							// FesStatement also can read record very fast.
						}
					}
				}
			}
		}
    }
}
#endif