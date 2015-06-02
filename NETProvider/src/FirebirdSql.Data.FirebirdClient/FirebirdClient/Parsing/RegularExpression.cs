
namespace FirebirdSql.Data.FirebirdClient.Parsing
{
	public class RegularExpression : SqlExpression
	{
		#region SqlExpression Members

		public void Evaluate(EvaluationContext context)
		{
			context.Output(context.CurrentToken);
		}

		#endregion
	}
}
