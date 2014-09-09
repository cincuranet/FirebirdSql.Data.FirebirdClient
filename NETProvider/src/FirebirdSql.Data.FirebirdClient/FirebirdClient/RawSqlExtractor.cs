using System;
using System.Text;

namespace FirebirdSql.Data.FirebirdClient
{
	/// <summary>
	/// Removes all comments from a sql statement
	/// </summary>
	public class RawSqlExtractor
	{
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
			var comment = Comment.GetFirst(sql);
			if (comment == null)
				result.Append(sql);
			else
				result.Append(RemoveComments(comment.Remove()));

			return result.ToString();
		}

		private abstract class Comment
		{
			protected const string LINE_COMMENT = "--";
			protected const string BLOCK_COMMENT_START = "/*";
			protected const string BLOCK_COMMENT_END = "*/";

			public abstract string Remove();

			public static Comment GetFirst(string sql)
			{
				var beginOfLineComment = sql.IndexOf(LINE_COMMENT);
				var beginOfBlockComment = sql.IndexOf(BLOCK_COMMENT_START);

				if (beginOfBlockComment != -1)
					if (beginOfLineComment == -1 || beginOfBlockComment < beginOfLineComment)
						return new BlockComment(sql);

				if (beginOfLineComment != -1)
					if (beginOfBlockComment == -1 || beginOfLineComment < beginOfBlockComment)
						return new LineComment(sql);

				return null;
			}
		}

		private class LineComment : Comment
		{
			private readonly string _sql;

			public LineComment(string sql)
			{
				_sql = sql;
			}

			public override string Remove()
			{
				var beginOfLineComment = _sql.IndexOf(LINE_COMMENT);
				var result = new StringBuilder();
				result.Append(_sql.Substring(0, beginOfLineComment));
				var endOfLineComment = _sql.IndexOf(Environment.NewLine, beginOfLineComment);
				if (endOfLineComment != -1)
					result.Append(_sql.Substring(endOfLineComment + Environment.NewLine.Length));

				return result.ToString();
			}
		}

		private class BlockComment : Comment
		{
            private readonly string _sql;

			public BlockComment(string sql)
			{
				_sql = sql;
			}

			public override string Remove()
			{
				var beginOfBlockComment = _sql.IndexOf(BLOCK_COMMENT_START);
				var result = new StringBuilder();
				result.Append(_sql.Substring(0, beginOfBlockComment));
				var endOfBlockComment = _sql.IndexOf(BLOCK_COMMENT_END, beginOfBlockComment);
				result.Append(_sql.Substring(endOfBlockComment + BLOCK_COMMENT_END.Length));

				return result.ToString();
			}
		}
	}
}
