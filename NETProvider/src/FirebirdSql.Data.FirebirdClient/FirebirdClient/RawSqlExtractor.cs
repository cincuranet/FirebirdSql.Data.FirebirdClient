using System;
using System.Text;

namespace FirebirdSql.Data.FirebirdClient
{
	/// <summary>
	/// Removes all comments from a sql statement
	/// </summary>
	public class RawSqlExtractor
	{
		private const string LINE_COMMENT = "--";
		private const string BLOCK_COMMENT_START = "/*";
		private const string BLOCK_COMMENT_END = "*/";
		private readonly string _sql;

		public RawSqlExtractor(string sql)
		{
			_sql = sql;			
		}

		public string Extract()
		{
			return RemoveComments(_sql);
		}

		private static string RemoveComments(string sql)
		{
			var result = new StringBuilder();
			var beginOfLineComment = sql.IndexOf(LINE_COMMENT);
			var beginOfBlockComment = sql.IndexOf(BLOCK_COMMENT_START);
			if (beginOfBlockComment > -1)
			{
				result.Append(sql.Substring(0, beginOfBlockComment));
				var endOfBlockComment = sql.IndexOf(BLOCK_COMMENT_END, beginOfBlockComment);
				result.Append(RemoveComments(sql.Substring(endOfBlockComment + BLOCK_COMMENT_END.Length)));
			}
			else
			{
				if (beginOfLineComment > -1)
				{
					result.Append(sql.Substring(0, beginOfLineComment));
					var endOfLineComment = sql.IndexOf(Environment.NewLine, beginOfLineComment);
					result.Append(RemoveComments(sql.Substring(endOfLineComment + Environment.NewLine.Length)));
				}
				else
					result.Append(sql);
			}
			return result.ToString();
		}
	}
}
