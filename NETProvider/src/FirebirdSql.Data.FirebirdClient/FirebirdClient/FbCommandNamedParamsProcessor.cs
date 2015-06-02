using System;
using System.Collections.Generic;
using FirebirdSql.Data.FirebirdClient.Parsing;

namespace FirebirdSql.Data.FirebirdClient
{
	public class FbCommandNamedParamsProcessor
	{
		private readonly List<string> _namedParameters;
		private readonly string _sql;

		public FbCommandNamedParamsProcessor(string sql, List<string> namedParameters)
		{
			_sql = sql;
			_namedParameters = namedParameters;
		}

		public string Process()
		{
			if (SqlHasNoNamedParams())
				return _sql;

			var context = new EvaluationContext(_sql, _namedParameters);
			while (context.HasTokensToProcess())
			{
				var expression = GetExpressionByToken(context.CurrentToken);
				expression.Evaluate(context);
				context.NextToken();
			}

			return context.Result;
		}

		private bool SqlHasNoNamedParams()
		{
			return _sql.IndexOf('@') == -1;
		}

		private static SqlExpression GetExpressionByToken(char token)
		{
			switch (token)
			{
				case '\'':
					return new QuotedExpression('\'');
				case '\"':
					return new QuotedExpression('\"');
				case '-':
					return new CommentExpression("--", Environment.NewLine);
				case '/':
					return new CommentExpression("/*", "*/");
				case '@':
					return new ParameterExpression();
				default:
					return new RegularExpression();
			}
		}
	}
}
