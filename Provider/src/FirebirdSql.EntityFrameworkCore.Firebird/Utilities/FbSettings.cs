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

//$Authors = Jiri Cincura (jiri@cincura.net), Rafael Almeida (ralms@ralms.net)

using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Utilities
{

	public class FbSettings
	{

		public Version ServerVersion;
		public bool IsSupportIdentityIncrement => ServerVersion.Major >= 3;
		public int ObjectLengthName => ServerVersion.Major == 3  ? 31 : 63;

		private static readonly ConcurrentDictionary<string, FbSettings> Settings = new ConcurrentDictionary<string, FbSettings>();

		public FbSettings GetSettings(string connectionString)
		{
			var csb = new FbConnectionStringBuilder(connectionString);

			return Settings.GetOrAdd(csb.ConnectionString, key =>
			{
				try
				{
					using (var _connection = new FbConnection(csb.ConnectionString))
					{
						_connection.Open();
						ServerVersion = Data.Services.FbServerProperties.ParseServerVersion(_connection.ServerVersion);
					}
				}
				catch
				{
					// ignored
				}
				return this;
			});
		}

		public FbSettings GetSettings(DbConnection connection)
		{
			var csb = new FbConnectionStringBuilder(connection.ConnectionString);

			return Settings.GetOrAdd(csb.ConnectionString, key =>
			{
				if (connection.State == ConnectionState.Closed)
				{
					connection.Open();
				}
				try
				{
					ServerVersion = Data.Services.FbServerProperties.ParseServerVersion(connection.ServerVersion);
					return this;
				}
				finally
				{
					if (connection?.State == ConnectionState.Open)
						connection.Close();
				}
			});
		}
	}
}
