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
using FirebirdSql.EntityFrameworkCore.Firebird.Infrastructure.Internal;
using FirebirdSql.EntityFrameworkCore.Firebird.Utilities;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Internal
{
	public class FbOptions : IFbOptions
	{ 
		private Lazy<FbSettings> _fbSettings;
		public virtual FbSettings Settings => _fbSettings.Value;

		public virtual void Initialize(IDbContextOptions options)
		{
			var fbOptions = GetOptions(options);
			_fbSettings = new Lazy<FbSettings>(() => fbOptions.Connection != null
				                                         ? new FbSettings().GetSettings(fbOptions.Connection)
				                                         : new FbSettings().GetSettings(fbOptions.ConnectionString));
		}

		public virtual void Validate(IDbContextOptions options)
		{
			var fbOptions = GetOptions(options);
		}
		 
		private FbOptionsExtension GetOptions(IDbContextOptions options)
			=> options.FindExtension<FbOptionsExtension>() ?? new FbOptionsExtension();
	}
}
