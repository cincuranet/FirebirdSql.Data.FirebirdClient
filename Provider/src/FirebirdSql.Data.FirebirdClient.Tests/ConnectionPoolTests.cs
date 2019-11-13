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

//$Authors = SynescGmbH

using System.Collections.Generic;
using FirebirdSql.Data.TestsBase;
using NUnit.Framework;

namespace FirebirdSql.Data.FirebirdClient.Tests
{
	[TestFixtureSource(typeof(FbDefaultServerTypeTestFixtureSource))]
	[TestFixtureSource(typeof(FbEmbeddedServerTypeTestFixtureSource))]
	public class ConnectionPoolTests : FbTestsBase
	{
		public ConnectionPoolTests(FbServerType serverType, bool compression, FbWireCrypt wireCrypt)
			: base(serverType, compression, wireCrypt)
		{ }

		[Test]
		public void BrokenConnectionHandling()
		{
			FbConnection.ClearAllPools();
			var csb = BuildConnectionStringBuilder(FbServerType, Compression, WireCrypt);
			csb.Pooling = true;
			csb.ConnectionLifeTime = 120;
			csb.MaxPoolSize = 10;
			var cs = csb.ToString();

			// get a stable connection to operate on
			var workConnection = new FbConnection(cs);
			workConnection.Open();

			// fill pool with connections
			var connections = new List<FbConnection>();
			for (var i = 1; i < csb.MaxPoolSize; i++)
			{
				var connection = new FbConnection(cs);
				connection.Open();
				connections.Add(connection);
			}

			// kill all open connection - simulates broken tcp connections
			using (var cmd = workConnection.CreateCommand())
			{
				cmd.CommandText = "delete from mon$attachments where mon$attachment_id <> current_connection";
				cmd.ExecuteScalar();
			}

			// use connections to test for crashed state
			foreach (var connection in connections.ToArray())
			{
				try
				{
					QuerySomething(connection);
				}
				catch
				{
					connection.Dispose();
					connections.Remove(connection);
				}
			}
			Assert.IsEmpty(connections);
      
			#region DNET-917 workaround
			// http://tracker.firebirdsql.org/browse/DNET-917
			// Extra loop on connections to really provoke crash

			for (var i = 1; i < csb.MaxPoolSize; i++)
			{
				var connection = new FbConnection(cs);
				connection.Open();
				connections.Add(connection);
			}

			foreach (var connection in connections.ToArray())
			{
				try
				{
					QuerySomething(connection);
				}
				catch
				{
					connection.Dispose();
					connections.Remove(connection);
				}
			}
			Assert.IsEmpty(connections);
			#endregion

			// create new connections
			try
			{
				for (var i = 1; i < csb.MaxPoolSize; i++)
				{
					var connection = new FbConnection(cs);
					connection.Open();
					connections.Add(connection);

					QuerySomething(connection);
				}
			}
			finally
			{
				connections.ForEach(x => x.Dispose());
				connections.Clear();
			}
		}

		private void QuerySomething(FbConnection connection)
		{
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = "select 1 from rdb$database";
				cmd.ExecuteScalar();
			}
		}
	}
}
