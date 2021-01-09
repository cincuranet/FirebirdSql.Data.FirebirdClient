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

using System;
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
	public class FbObjectToStringTranslator : IMethodCallTranslator
	{
		static readonly HashSet<Type> SupportedTypes = new HashSet<Type>
		{
			typeof(int),
			typeof(long),
			typeof(DateTime),
			typeof(bool),
			typeof(byte),
			typeof(byte[]),
			typeof(double),
			typeof(char),
			typeof(short),
			typeof(float),
			typeof(decimal),
			typeof(TimeSpan),
			typeof(uint),
			typeof(ushort),
			typeof(ulong),
			typeof(sbyte),
		};

		readonly FbSqlExpressionFactory _fbSqlExpressionFactory;

		public FbObjectToStringTranslator(FbSqlExpressionFactory fbSqlExpressionFactory)
		{
			_fbSqlExpressionFactory = fbSqlExpressionFactory;
		}

#if NETSTANDARD2_0
		public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
#else
		public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
#endif
		{
			if (method.Name == nameof(ToString) && method.GetParameters().Length == 0)
			{
				var type = instance.Type.UnwrapNullableType();
				if (SupportedTypes.Contains(type))
				{
					return _fbSqlExpressionFactory.Convert(instance, typeof(string));
				}
				else if (type == typeof(Guid))
				{
					return _fbSqlExpressionFactory.Function("UUID_TO_CHAR", new[] { instance }, typeof(string));
				}
			}
			return null;
		}
	}
}
