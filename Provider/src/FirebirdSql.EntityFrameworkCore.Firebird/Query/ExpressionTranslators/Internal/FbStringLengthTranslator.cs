﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/blob/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$Authors = Jiri Cincura (jiri@cincura.net), Jean Ressouche, Rafael Almeida (ralms@ralms.net)

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Query.ExpressionTranslators.Internal
{
	public class FbStringLengthTranslator : IMemberTranslator
	{
		public virtual Expression Translate(MemberExpression memberExpression)
		{
			if (memberExpression.Expression != null && memberExpression.Expression.Type == typeof(string) && memberExpression.Member.Name == nameof(string.Length))
			{
				return new ExplicitCastExpression(
					new SqlFunctionExpression("CHARACTER_LENGTH", memberExpression.Type, new[] { memberExpression.Expression }),
					typeof(int));
			}
			return null;
		}
	}
}
