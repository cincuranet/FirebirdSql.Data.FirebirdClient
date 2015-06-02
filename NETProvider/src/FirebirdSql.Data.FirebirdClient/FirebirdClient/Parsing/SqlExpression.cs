using System;

namespace FirebirdSql.Data.FirebirdClient.Parsing
{
	public interface SqlExpression
	{
		void Evaluate(EvaluationContext context);
	}
}