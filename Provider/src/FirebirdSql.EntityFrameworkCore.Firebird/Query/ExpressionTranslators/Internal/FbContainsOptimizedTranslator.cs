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

//$Authors = Jiri Cincura (jiri@cincura.net)

using System.Collections.Generic;
using System.Reflection;
using FirebirdSql.EntityFrameworkCore.Firebird.Query.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
#if !NETSTANDARD2_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
#endif

namespace FirebirdSql.EntityFrameworkCore.Firebird.Query.ExpressionTranslators.Internal
{
	public class FbContainsOptimizedTranslator : IMethodCallTranslator
	{
		static readonly MethodInfo MethodInfo = typeof(string).GetRuntimeMethod(nameof(string.Contains), new[] { typeof(string) });

		readonly FbSqlExpressionFactory _fbSqlExpressionFactory;

		public FbContainsOptimizedTranslator(FbSqlExpressionFactory fbSqlExpressionFactory)
		{
			_fbSqlExpressionFactory = fbSqlExpressionFactory;
		}
#if NETSTANDARD2_0
		public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
#else
		public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
#endif
		
		{
			if (!method.Equals(MethodInfo))
				return null;

			var patternExpression = arguments[0];
			var positionExpression = _fbSqlExpressionFactory.GreaterThan(
				_fbSqlExpressionFactory.Function(
					"POSITION",
					new[] { patternExpression, instance },
					typeof(int)),
				_fbSqlExpressionFactory.Constant(0));
			return patternExpression is SqlConstantExpression sqlConstantExpression
				? ((string)sqlConstantExpression.Value)?.Length == 0
					? (SqlExpression)_fbSqlExpressionFactory.Constant(true)
					: positionExpression
				: _fbSqlExpressionFactory.OrElse(
					positionExpression,
					_fbSqlExpressionFactory.Equal(
						patternExpression,
						_fbSqlExpressionFactory.Constant(string.Empty)));
		}
	}
}
