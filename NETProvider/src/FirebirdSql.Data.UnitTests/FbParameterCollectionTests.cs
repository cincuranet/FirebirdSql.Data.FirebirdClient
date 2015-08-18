/*
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

using System;

using FirebirdSql.Data.FirebirdClient;
using NUnit.Framework;

namespace FirebirdSql.Data.UnitTests
{
	[TestFixture(FbServerType.Default)]
	[TestFixture(FbServerType.Embedded)]
	public class FbParameterCollectionTests : TestsBase
	{
		#region Constructors

		public FbParameterCollectionTests(FbServerType serverType)
			: base(serverType)
		{
		}

		#endregion

		#region Unit Tests

		[Test]
		public void AddTest()
		{
			FbCommand command = new FbCommand();

			command.Parameters.Add(new FbParameter("@p292", 10000));
			command.Parameters.Add("@p01", FbDbType.Integer);
			command.Parameters.Add("@p02", 289273);
			command.Parameters.Add("#p3", FbDbType.SmallInt, 2, "sourceColumn");
		}

		#endregion
	}
}
