using System;
using System.Text;

namespace FirebirdSql.Data.FirebirdClient.Parsing
{
	public class ParameterExpression : SqlExpression
	{
		#region SqlExpression Members

		public void Evaluate(EvaluationContext context)
		{
			var paramBuilder = new StringBuilder("@");

			for (int i = context.CurrentIndex + 1; i < context.Input.Length; i++)
			{
				var token = context.Input[i];
				if (Char.IsLetterOrDigit(token) || token == '_' || token == '$')
				{
					paramBuilder.Append(token);
				}
				else
				{
					context.NamedParameters.Add(paramBuilder.ToString());
					context.Output('?');
					context.Output(token);
					context.MoveTo(i);
					return;
				}
			}

			context.NamedParameters.Add(paramBuilder.ToString());
			context.Output('?');
			context.MoveTo(context.Input.Length);
		}

		#endregion
	}
}
