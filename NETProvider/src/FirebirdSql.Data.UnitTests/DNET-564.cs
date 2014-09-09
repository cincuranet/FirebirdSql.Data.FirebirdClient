using FirebirdSql.Data.FirebirdClient;
using NUnit.Framework;

namespace FirebirdSql.Data.UnitTests
{
	[TestFixture]
	// http://tracker.firebirdsql.org/browse/DNET-564
	public class Dnet564 : TestsBase
	{
		public Dnet564()
			: base(false)
		{
		}
		[Test]
		public void VerySimpleQuery()
		{
			using (var command = new FbCommand(@"select count(int_field) from test"))
			{
				command.Connection = Connection;
				command.Transaction = Connection.BeginTransaction();
				var result = command.ExecuteScalar();
				Assert.That(result, Is.GreaterThan(0));
			}
		}

		[Test]
		public void SimpleQueryWithOneParam()
		{
			using (var command = new FbCommand(@"select count(int_field) from test where varchar_field = @p0"))
			{
				command.Connection = Connection;
				command.Transaction = Connection.BeginTransaction();
				command.Parameters.Add("@p0", FbDbType.Text).Value = "IRow Number 0";
				var result = command.ExecuteScalar();
				Assert.That(result, Is.EqualTo(1));
			}			
		}

		[Test]
		public void SimpleQueryWithMoreThanOneParam()
		{
			using (var command = new FbCommand(@"select count(int_field) from test where varchar_field = @p0 and int_field < @p1"))
			{
				command.Connection = Connection;
				command.Transaction = Connection.BeginTransaction();
				command.Parameters.Add("@p0", FbDbType.Text).Value = "IRow Number 0";
				command.Parameters.Add("@p1", FbDbType.Integer).Value = 10;
				var result = command.ExecuteScalar();
				Assert.That(result, Is.EqualTo(1));
			}
		}

		[Test]
		public void AcceptsParamsWhenQueryContainsALineCommentWithASingleQuote()
		{
			using (var command = new FbCommand(@"select count(int_field)
                                                 from test
			                                     -- comment with '
                                                 where varchar_field = @p0"))
			{
				command.Connection = Connection;
				command.Transaction = Connection.BeginTransaction();
				command.Parameters.Add("@p0", FbDbType.Text).Value = "IRow Number 0";
				command.ExecuteScalar();
				var result = command.ExecuteScalar();
				Assert.That(result, Is.EqualTo(1));
			}
		}

		[Test]
		public void AcceptsParamsWhenQueryContainsCLikeCommentWithASingleQuote()
		{
			using (var command = new FbCommand(@"select count(int_field)
                                                 from test /* this is a comment with ' */
                                                 where varchar_field = @p0"))
			{
				command.Connection = Connection;
				command.Transaction = Connection.BeginTransaction();
				command.Parameters.Add("@p0", FbDbType.Text).Value = "IRow Number 0";
				command.ExecuteScalar();
				var result = command.ExecuteScalar();
				Assert.That(result, Is.EqualTo(1));
			}
		}

		[Test]
		public void AcceptsParamsWhenQueryContainsALineCommentWithAParamToken()
		{
			using (var command = new FbCommand(@"select count(int_field)
                                                 from test
			                                     -- comment with @p0
                                                 where varchar_field = @p0"))
			{
				command.Connection = Connection;
				command.Transaction = Connection.BeginTransaction();
				command.Parameters.Add("@p0", FbDbType.Text).Value = "IRow Number 0";
				command.ExecuteScalar();
				var result = command.ExecuteScalar();
				Assert.That(result, Is.EqualTo(1));
			}
		}

		[Test]
		public void AcceptsParamsWhenQueryContainsCLikeCommentWithAParamToken()
		{
			using (var command = new FbCommand(@"select count(int_field)
                                                 from test /* this is a comment with @p0 */
                                                 where varchar_field = @p0"))
			{
				command.Connection = Connection;
				command.Transaction = Connection.BeginTransaction();
				command.Parameters.Add("@p0", FbDbType.Text).Value = "IRow Number 0";
				command.ExecuteScalar();
				var result = command.ExecuteScalar();
				Assert.That(result, Is.EqualTo(1));
			}
		}

		[Test]
		public void ShouldParseQueryThatContainBothCommentKindsWithAParamToken()
		{
			using (var command = new FbCommand(@"select count(int_field)
                                                 from test 
                                                 /* 
                                                 this is a comment with '
                                                 where varchar_field = @p0
                                                 */"))
			{
				command.Connection = Connection;
				command.Transaction = Connection.BeginTransaction();
				command.Parameters.Add("@p0", FbDbType.Text).Value = "IRow Number 0";
				command.ExecuteScalar();
				var result = command.ExecuteScalar();
				Assert.That(result, Is.GreaterThan(0));
			}
		}
	}
}
