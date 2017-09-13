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

using System;
using System.Data.Common;
using FirebirdSql.EntityFrameworkCore.Firebird.Infrastructure;
using FirebirdSql.EntityFrameworkCore.Firebird.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Extensions
{
	public static class FbDbContextOptionsExtensions
	{
		public static DbContextOptionsBuilder UseFirebird(this DbContextOptionsBuilder optionsBuilder, string connectionString, Action<FbDbContextOptionsBuilder> fbOptionsAction = null)
		{
			var extension = GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);
			((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
			fbOptionsAction?.Invoke(new FbDbContextOptionsBuilder(optionsBuilder));
			return optionsBuilder;
		}

		public static DbContextOptionsBuilder UseFirebird(this DbContextOptionsBuilder optionsBuilder, DbConnection connection, Action<FbDbContextOptionsBuilder> fbOptionsAction = null)
		{
			var extension = GetOrCreateExtension(optionsBuilder).WithConnection(connection);
			((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
			fbOptionsAction?.Invoke(new FbDbContextOptionsBuilder(optionsBuilder));
			return optionsBuilder;
		}

		public static DbContextOptionsBuilder<TContext> UseFirebird<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, string connectionString, Action<FbDbContextOptionsBuilder> fbOptionsAction = null)
			where TContext : DbContext
		{
			return (DbContextOptionsBuilder<TContext>)UseFirebird((DbContextOptionsBuilder)optionsBuilder, connectionString, fbOptionsAction);
		}

		public static DbContextOptionsBuilder<TContext> UseFirebird<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, DbConnection connection, Action<FbDbContextOptionsBuilder> fbOptionsAction = null)
			where TContext : DbContext
		{
			return (DbContextOptionsBuilder<TContext>)UseFirebird((DbContextOptionsBuilder)optionsBuilder, connection, fbOptionsAction);
		}

		static FbOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
		{
			var existsExtension = optionsBuilder.Options.FindExtension<FbOptionsExtension>();
			return existsExtension != null
				? new FbOptionsExtension(existsExtension)
				: new FbOptionsExtension();
		}
	}
}
