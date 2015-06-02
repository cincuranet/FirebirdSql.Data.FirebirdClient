namespace FirebirdSql.Data.FirebirdClient.Parsing
{
	public class QuotedExpression : SqlExpression
	{
		private readonly char _quote;

		public QuotedExpression(char quote)
		{
			_quote = quote;
		}

		#region SqlExpression Members

		public void Evaluate(EvaluationContext context)
		{
			var indexOfClosingQuote = context.Input.IndexOf(_quote, context.CurrentIndex + 1);

			for (int i = context.CurrentIndex; i <= indexOfClosingQuote; i++)
			{
				var token = context.Input[i];
				context.Output(token);
			}

			context.MoveTo(indexOfClosingQuote);
		}

		#endregion
	}
}
