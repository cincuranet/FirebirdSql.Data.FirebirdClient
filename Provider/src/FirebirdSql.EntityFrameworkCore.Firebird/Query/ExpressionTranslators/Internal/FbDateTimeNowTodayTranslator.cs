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
	public class FbDateTimeNowTodayTranslator : IMemberTranslator
	{
		readonly FbSqlExpressionFactory _fbSqlExpressionFactory;

		public FbDateTimeNowTodayTranslator(FbSqlExpressionFactory fbSqlExpressionFactory)
		{
			_fbSqlExpressionFactory = fbSqlExpressionFactory;
		}

#if NETSTANDARD2_0
		public SqlExpression Translate(SqlExpression instance, MemberInfo member, Type returnType)
#else
		public SqlExpression Translate(SqlExpression instance, MemberInfo member, Type returnType, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
#endif
		{
			if (member.DeclaringType == typeof(DateTime) && member.Name == nameof(DateTime.Now))
			{
				// LOCALTIMESTAMP
				return _fbSqlExpressionFactory.Function("CURRENT_TIMESTAMP", typeof(DateTime));
			}
			if (member.DeclaringType == typeof(DateTime) && member.Name == nameof(DateTime.Today))
			{
				return _fbSqlExpressionFactory.Function("CURRENT_DATE", typeof(DateTime));
			}
			return null;
		}
	}
}
