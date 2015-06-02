
namespace FirebirdSql.Data.FirebirdClient.Parsing
{
	public class CommentExpression : SqlExpression
	{
		private readonly string _commentBegin;
		private readonly string _commentEnd;

		public CommentExpression(string commentBegin, string commentEnd)
		{
			_commentBegin = commentBegin;
			_commentEnd = commentEnd;
		}

		#region SqlExpression Members

		public void Evaluate(EvaluationContext context)
		{
			var nextIndex = context.CurrentIndex + 1;
			var nextChar = nextIndex < context.Input.Length ? context.Input[nextIndex] : '\0';
			if (IsNotACommentBegin(context, nextChar))
			{
				context.Output(context.CurrentToken);
				return;
			}

			context.Output(context.CurrentToken);
			var indexOfCommentClosure = context.Input.IndexOf(_commentEnd, nextIndex);
			// no comment closure detected? treat the rest of the string as part of the comment(!)
			if (indexOfCommentClosure == -1)
				indexOfCommentClosure = context.Input.Length;

			for (int i = nextIndex; i < indexOfCommentClosure; i++)
			{
				var token = context.Input[i];
				context.Output(token);
				context.MoveTo(i);
			}
		}

		#endregion

		private bool IsNotACommentBegin(EvaluationContext context, char nextChar)
		{
			return string.Format("{0}{1}", context.CurrentToken, nextChar) != _commentBegin;
		}
	}
}