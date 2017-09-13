/*
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
using System.Text; 
using FirebirdSql.EntityFrameworkCore.Firebird.Extensions;
using FirebirdSql.EntityFrameworkCore.Firebird.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Storage.Internal
{
	public class FbSqlGenerationHelper : RelationalSqlGenerationHelper
	{
		private readonly IFbOptions _options;

		public FbSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies, IFbOptions options)
			: base(dependencies)
		{
			_options = options;
		}

		public override string EscapeIdentifier(string identifier)
		{
			return identifier.MaxLength(_options.Settings.ObjectLengthName);
		}

		public override void EscapeIdentifier(StringBuilder builder, string identifier)
		{ 
			builder.Append(identifier.MaxLength(_options.Settings.ObjectLengthName));
		}

		public override string DelimitIdentifier(string identifier)
		{
			return $"\"{EscapeIdentifier(identifier)}\"";
		}

		public override void DelimitIdentifier(StringBuilder builder, string identifier)
		{
			builder.Append('"');
			EscapeIdentifier(builder, identifier.MaxLength(_options.Settings.ObjectLengthName));
			builder.Append('"');
		}

		public override string GenerateParameterName(string name)
		{
			return $"@{name.MaxLength(_options.Settings.ObjectLengthName)}";
		}

		public override void GenerateParameterName(StringBuilder builder, string name)
		{
			builder.Append("@").Append(name.MaxLength(_options.Settings.ObjectLengthName));
		}
	}
}
