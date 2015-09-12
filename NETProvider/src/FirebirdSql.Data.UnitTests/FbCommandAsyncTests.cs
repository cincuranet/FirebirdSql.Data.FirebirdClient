#if NET_45
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using System.Threading;
using System.Data.Common;

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
			string query = "SELECT FIRST(1) varchar_field from test";
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
			string query = "SELECT FIRST(1) varchar_field from test";
			using (CancellationTokenSource cts = new CancellationTokenSource())
			using (var command = this.Connection.CreateCommand())
			{
				command.CommandText = query;
				Task<object> ret = command.ExecuteScalarAsync(cts.Token);
				cts.Cancel();
				AssertOperationCancelledException(ret);
			}
		}
		
		[Test]
		public void TestCancellationBeforeExecuteScalarAsync()
		{
			string query = "SELECT FIRST(1) int_field from test";
			using (var command = this.Connection.CreateCommand())
			{
				command.CommandText = query;
				CancellationTokenSource cts = new CancellationTokenSource();
				cts.Cancel();
				Task<object> ret = command.ExecuteScalarAsync(cts.Token);				
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
			string query = "SELECT COUNT(*) int_field from test";
			using (var command = this.Connection.CreateCommand())
			{
				command.CommandText = query;
				CancellationTokenSource cts = new CancellationTokenSource();
				Task<int> ret = command.ExecuteNonQueryAsync(cts.Token);
				cts.Cancel();
				AssertOperationCancelledException(ret);
			}
		}

		[Test]
		public void TestCancellationBeforeExecuteNonQueryAsync()
		{
			string query = "SELECT FIRST(1) int_field from test";
			using (var command = this.Connection.CreateCommand())
			{
				command.CommandText = query;
				CancellationTokenSource cts = new CancellationTokenSource();
				cts.Cancel();
				Task<int> ret = command.ExecuteNonQueryAsync(cts.Token);
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
				Assert.IsTrue(ex.InnerException is OperationCanceledException, ex.InnerException.GetType().Name + ":" + ex.InnerException.Message + ex.InnerException.StackTrace);
				return;
			}

			Assert.Inconclusive("Execution performed faster than cancellation");
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

		[Test]
		public void TestCancellationBeforeExecuteReaderAsync()
		{
			CancellationTokenSource cts = new CancellationTokenSource();
			cts.Cancel();

			using (FbCommand command = new FbCommand("select * from TEST", Connection))
			{
				var readerOpenTask = command.ExecuteReaderAsync(cts.Token);
				AssertOperationCancelledException(readerOpenTask);
			}
		}
		
		[Test]
		public void TestCancellationExecuteReaderAsync()
		{
			CancellationTokenSource ctsCancelled = new CancellationTokenSource();
			ctsCancelled.Cancel();
			CancellationTokenSource cts = new CancellationTokenSource();
			using (FbTransaction transaction = Connection.BeginTransaction())
			{
				using (FbCommand command = new FbCommand("select * from TEST", Connection, transaction))
				{
					// Check cancelling creating reader
					Task<DbDataReader> readerOpenTask;
					{
						readerOpenTask = command.ExecuteReaderAsync(cts.Token);
						cts.Cancel();
						AssertOperationCancelledException(readerOpenTask);
					}

					readerOpenTask = command.ExecuteReaderAsync();
					using (var reader = readerOpenTask.Result)
					{
						// Test before call cancelled for Read
						var readTask = reader.ReadAsync(ctsCancelled.Token);
						AssertOperationCancelledException(readTask);

						cts = new CancellationTokenSource();
						readTask = reader.ReadAsync();
						cts.Cancel();
						AssertOperationCancelledException(readTask);
					}
				}
			}
		}
	}
}
#endif