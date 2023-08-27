﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/raw/master/license.txt.
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
using System.Linq;
using System.Reflection;
using FirebirdSql.EntityFrameworkCore.Firebird.Query.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Query.ExpressionTranslators.Internal;

public class FbStringIndexOfTranslator : IMethodCallTranslator
{
	static readonly MethodInfo IndexOfMethodInfo
	   = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), new[] { typeof(string) });

	static readonly MethodInfo IndexOfMethodInfoWithStartingPosition
		= typeof(string).GetRuntimeMethod(nameof(string.IndexOf), new[] { typeof(string), typeof(int) });

	readonly FbSqlExpressionFactory _fbSqlExpressionFactory;

	public FbStringIndexOfTranslator(FbSqlExpressionFactory fbSqlExpressionFactory)
	{
		_fbSqlExpressionFactory = fbSqlExpressionFactory;
	}

	public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
	{
		if (method.Equals(IndexOfMethodInfo))
		{
			var args = new List<SqlExpression>();
			args.Add(_fbSqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]));
			args.Add(instance);
			return _fbSqlExpressionFactory.Subtract(
				_fbSqlExpressionFactory.Function("POSITION", args, true, Enumerable.Repeat(true, args.Count), typeof(int)),
				_fbSqlExpressionFactory.Constant(1));
		}
		if (method.Equals(IndexOfMethodInfoWithStartingPosition))
		{
			var args = new List<SqlExpression>();
			args.Add(_fbSqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]));
			args.Add(instance);
			args.Add(_fbSqlExpressionFactory.Add(arguments[1], _fbSqlExpressionFactory.Constant(1)));
			return _fbSqlExpressionFactory.Subtract(
				_fbSqlExpressionFactory.Function("POSITION", args, true, Enumerable.Repeat(true, args.Count), typeof(int)),
				_fbSqlExpressionFactory.Constant(1));
		}
		return null;
	}
}
