﻿/*
 *  Firebird ADO.NET Data provider for .NET and Mono
 *
 *     The contents of this file are subject to the Initial
 *     Developer's Public License Version 1.0 (the "License");
 *     you may not use this file except in compliance with the
 *     License. You may obtain a copy of the License at
 *     http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *     Software distributed under the License is distributed on
 *     an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *     express or implied.  See the License for the specific
 *     language governing rights and limitations under the License.
 *
 *  Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

using System.Data;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.TestsBase;
using NUnit.Framework;

namespace FirebirdSql.Data.FirebirdClient.Tests
{
	[FbTestFixture(FbServerType.Default, false)]
	[FbTestFixture(FbServerType.Default, true)]
	[FbTestFixture(FbServerType.Embedded, default(bool))]
	public class FbStoredProcCallsTests : FbTestsBase
	{
		#region Constructors

		public FbStoredProcCallsTests(FbServerType serverType, bool compression)
			: base(serverType, compression)
		{ }

		#endregion

		#region Unit Tests

		[Test]
		public void FirebirdLikeTest00()
		{
			FbCommand command = new FbCommand("EXECUTE PROCEDURE GETVARCHARFIELD(?)", Connection);

			command.CommandType = CommandType.StoredProcedure;

			command.Parameters.Add("@ID", FbDbType.VarChar).Direction = ParameterDirection.Input;
			command.Parameters.Add("@VARCHAR_FIELD", FbDbType.VarChar).Direction = ParameterDirection.Output;

			command.Parameters[0].Value = 1;

			// This will fill output parameters values
			command.ExecuteNonQuery();

			// Check the value
			Assert.AreEqual("IRow Number 1", command.Parameters[1].Value);

			// Dispose command - this will do a transaction commit
			command.Dispose();
		}

		[Test]
		public void FirebirdLikeTest01()
		{
			FbCommand command = new FbCommand("SELECT * FROM GETVARCHARFIELD(?)", Connection);
			command.CommandType = CommandType.StoredProcedure;

			command.Parameters.Add("@ID", FbDbType.VarChar).Direction = ParameterDirection.Input;
			command.Parameters[0].Value = 1;

			// This will fill output parameters values
			FbDataReader reader = command.ExecuteReader();
			reader.Read();

			// Print output value
			TestContext.WriteLine("Output Parameters - Result of SELECT command");
			TestContext.WriteLine(reader[0]);

			reader.Close();

			// Dispose command - this will do a transaction commit
			command.Dispose();
		}

		[Test]
		public void SqlServerLikeTest00()
		{
			FbCommand command = new FbCommand("GETVARCHARFIELD", Connection);

			command.CommandType = CommandType.StoredProcedure;

			command.Parameters.Add("@ID", FbDbType.VarChar).Direction = ParameterDirection.Input;
			command.Parameters.Add("@VARCHAR_FIELD", FbDbType.VarChar).Direction = ParameterDirection.Output;

			command.Parameters[0].Value = 1;

			// This will fill output parameters values
			command.ExecuteNonQuery();

			// Print output value
			TestContext.WriteLine("Output Parameters");
			TestContext.WriteLine(command.Parameters[1].Value);

			// Dispose command - this will do a transaction commit
			command.Dispose();
		}

		[Test]
		public void SqlServerLikeTest01()
		{
			FbCommand command = new FbCommand("GETRECORDCOUNT", Connection);
			command.CommandType = CommandType.StoredProcedure;

			command.Parameters.Add("@RECORDCOUNT", FbDbType.Integer).Direction = ParameterDirection.Output;

			// This will fill output parameters values
			command.ExecuteNonQuery();

			// Print output value
			TestContext.WriteLine("Output Parameters - Record Count");
			TestContext.WriteLine(command.Parameters[0].Value);

			// Dispose command - this will do a transaction commit
			command.Dispose();
		}

		[Test]
		public void SqlServerLikeTest02()
		{
			FbCommand command = new FbCommand("GETVARCHARFIELD", Connection);

			command.CommandType = CommandType.StoredProcedure;

			command.Parameters.Add("@ID", FbDbType.VarChar).Value = 1;

			// This will fill output parameters values
			FbDataReader r = command.ExecuteReader();

			int count = 0;

			while (r.Read())
			{
				count++;
			}

			r.Close();

			// Dispose command - this will do a transaction commit
			command.Dispose();

			Assert.AreEqual(1, count);
		}

		#endregion
	}
}
