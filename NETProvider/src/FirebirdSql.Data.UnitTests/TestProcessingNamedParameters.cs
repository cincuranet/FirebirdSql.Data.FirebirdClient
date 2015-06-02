using System.Collections.Generic;
using System.Text;
using FirebirdSql.Data.FirebirdClient;
using NUnit.Framework;

namespace FirebirdSql.Data.UnitTests
{
	[TestFixture]
	public class TestProcessingNamedParameters
	{
		[Test]
		public void NoNamedParameters_ReturnsOriginSql()
		{
			var namedParams = new List<string>();
			var expected = "select * from table";
			var processor = new FbCommandNamedParamsProcessor(expected, namedParams);

			var actual = processor.Process();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(0));
		}

		[Test]
		public void ContainsOneNamedParam_ReplacesNamedParamWithPositionalParam()
		{
			var namedParams = new List<string>();
			var processor = new FbCommandNamedParamsProcessor("select * from table where col = @p0", namedParams);

			var result = processor.Process();

			Assert.That(result, Is.EqualTo("select * from table where col = ?"));
			Assert.That(namedParams.Count, Is.EqualTo(1));
			Assert.That(namedParams[0], Is.EqualTo("@p0"));
		}

		[Test]
		public void ContainsMultipleNamedParams_ReplacesAllNamedParamsWithPositionalParams()
		{
			var namedParams = new List<string>();
			var processor = new FbCommandNamedParamsProcessor("select * from table where col = @p0 and col1 = @p1 and col2 = @p2", namedParams);

			var result = processor.Process();

			Assert.That(result, Is.EqualTo("select * from table where col = ? and col1 = ? and col2 = ?"));
			Assert.That(namedParams.Count, Is.EqualTo(3));
			Assert.That(namedParams[0], Is.EqualTo("@p0"));
			Assert.That(namedParams[1], Is.EqualTo("@p1"));
			Assert.That(namedParams[2], Is.EqualTo("@p2"));
		}

		[Test]
		public void ContainsEmailAddressWithinQuotesAndNoParams_ReturnsOriginSql()
		{
			var namedParams = new List<string>();
			var expected = "update table set email = 'me@myhome.world'";
			var processor = new FbCommandNamedParamsProcessor(expected, namedParams);

			var actual = processor.Process();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(0));
		}

		[Test]
		public void ContainsEmailAddressWithinDoubleQuotesAndNoParams_ReturnsOriginSql()
		{
			var namedParams = new List<string>();
			var expected = "update table set email = \"me@myhome.world\"";
			var processor = new FbCommandNamedParamsProcessor(expected, namedParams);

			var actual = processor.Process();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(0));
		}

		[Test]
		public void ContainsEmailAddressWithinQuotesAndOneParam_ReturnsPositionalParam()
		{
			var namedParams = new List<string>();
			var processor = new FbCommandNamedParamsProcessor("update table set email = 'me@myhome.world', col = @param", namedParams);

			var actual = processor.Process();
			Assert.That(actual, Is.EqualTo("update table set email = 'me@myhome.world', col = ?"));
			Assert.That(namedParams.Count, Is.EqualTo(1));
		}

		[Test]
		public void ContainsParamWithinALineComment_ReturnsOriginalSql()
		{
			var namedParams = new List<string>();
			var expected = "update table set --email = @params";
			var processor = new FbCommandNamedParamsProcessor(expected, namedParams);

			var actual = processor.Process();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(0));
		}

		[Test]
		public void ContainsParamWithinABlockComment_ReturnsOriginalSql()
		{
			var namedParams = new List<string>();
			var expected = "update table set /*email = @params*/";
			var processor = new FbCommandNamedParamsProcessor(expected, namedParams);

			var actual = processor.Process();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(0));
		}

		[Test]
		public void ContainsLineCommentWithinQuotes_ReturnsOriginSql()
		{
			var namedParams = new List<string>();
			var expected = "update table set '--email = @params'";
			var processor = new FbCommandNamedParamsProcessor(expected, namedParams);

			var actual = processor.Process();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(0));
		}

		[Test]
		public void ContainsParamWithinQuotes_ReturnsOriginSql()
		{
			var namedParams = new List<string>();
			var expected = "update table set col = '@p0'";
			var processor = new FbCommandNamedParamsProcessor(expected, namedParams);

			var actual = processor.Process();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(0));
		}

		[Test]
		public void ContainsLineCommentWithinABlockComment_ReturnsCorrectSql()
		{
			var namedParams = new List<string>();
			var sql = new StringBuilder()
				.AppendLine("select *")
				.AppendLine("from table")
				.AppendLine("where")
				.AppendLine("/*")
				.AppendLine("-- a comment with a '")
				.AppendLine("*/")
				.Append("col = @p0")
				.ToString();
			var processor = new FbCommandNamedParamsProcessor(sql, namedParams);

			var actual = processor.Process();

			var expected = new StringBuilder()
				.AppendLine("select *")
				.AppendLine("from table")	
				.AppendLine("where")
				.AppendLine("/*")
				.AppendLine("-- a comment with a '")
				.AppendLine("*/")
				.Append("col = ?")
				.ToString();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(1));
			Assert.That(namedParams[0], Is.EqualTo("@p0"));
		}

		[Test]
		public void ContainsLineCommentWithApostroph_ReturnsCorrectSql()
		{
			var sql = new StringBuilder()
				.AppendLine("select *")
				.AppendLine("from table")
				.AppendLine("-- comment with '")
				.Append("where col = @p0")
				.ToString();
			var namedParams = new List<string>();
			var processor = new FbCommandNamedParamsProcessor(sql, namedParams);

			var actual = processor.Process();

			var expected = new StringBuilder()
				.AppendLine("select *")
				.AppendLine("from table")
				.AppendLine("-- comment with '")
				.Append("where col = ?")
				.ToString();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(1));
			Assert.That(namedParams[0], Is.EqualTo("@p0"));
		}

		[Test]
		public void ContainsBlockCommentWithApostroph_ReturnsCorrectSql()
		{
			var sql = new StringBuilder()
				.AppendLine("select *")
				.AppendLine("from table")
				.AppendLine("/* comment with ' */")
				.Append("where col = @p0")
				.ToString();
			var namedParams = new List<string>();
			var processor = new FbCommandNamedParamsProcessor(sql, namedParams);

			var actual = processor.Process();

			var expected = new StringBuilder()
				.AppendLine("select *")
				.AppendLine("from table")
				.AppendLine("/* comment with ' */")
				.Append("where col = ?")
				.ToString();
			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(1));
			Assert.That(namedParams[0], Is.EqualTo("@p0"));
		}

		[Test]
		public void ContainsCommentedOutBlockCommentEndWithApostroph_ReturnsCorrectSql()
		{
			var sql = new StringBuilder()
				.AppendLine("select *")
				.AppendLine("from table")
				.AppendLine("/*")
				.AppendLine("-- comment with '")
				.AppendLine("where col = @p0")
				.Append("--*/")
				.ToString();
			var namedParams = new List<string>();
			var processor = new FbCommandNamedParamsProcessor(sql, namedParams);

			var actual = processor.Process();

			Assert.That(actual, Is.EqualTo(sql));
			Assert.That(namedParams.Count, Is.EqualTo(0));
		}

		[Test]
		public void ContainsCommentedOutBlockComment_ReturnsCorrectSql()
		{
			var sql = new StringBuilder()
				.AppendLine("select *")
				.AppendLine("from table")
				.AppendLine("--/*")
				.AppendLine("-- comment with '")
				.AppendLine("where col = @p0")
				.Append("--*/")
				.ToString();
			var namedParams = new List<string>();
			var processor = new FbCommandNamedParamsProcessor(sql, namedParams);

			var actual = processor.Process();

			var expected = new StringBuilder()
				.AppendLine("select *")
				.AppendLine("from table")
				.AppendLine("--/*")
				.AppendLine("-- comment with '")
				.AppendLine("where col = ?")
				.Append("--*/")
				.ToString();

			Assert.That(actual, Is.EqualTo(expected));
			Assert.That(namedParams.Count, Is.EqualTo(1));
			Assert.That(namedParams[0], Is.EqualTo("@p0"));
		}
	}
}
