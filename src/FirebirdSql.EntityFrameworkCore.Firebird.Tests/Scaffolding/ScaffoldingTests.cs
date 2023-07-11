/*
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

using System;
using System.Linq;
using System.Threading.Tasks;
using FirebirdSql.EntityFrameworkCore.Firebird.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using NUnit.Framework;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Tests.Scaffolding;
#pragma warning disable EF1001
public class ScaffoldingTests : EntityFrameworkCoreTestsBase
{
	private DatabaseModel _databaseModel;

	public override async Task SetUp()
	{
		await base.SetUp();

		try
		{
			var modelFactory = GetModelFactory();
			_databaseModel = modelFactory.Create(Connection, new DatabaseModelFactoryOptions());
		}
		catch (Exception)
		{
			_databaseModel = null;
		}
	}

	[Test]
	public void JustCanRun()
	{
		Assert.NotNull(_databaseModel);
	}

	[Test]
	public void CanScaffoldPrimaryKey()
	{
		var testTable = _databaseModel.Tables.Where(t => t.Name.Equals("TEST")).First();

		Assert.NotNull(testTable.PrimaryKey);
		Assert.AreEqual("INT_FIELD", testTable.PrimaryKey.Columns[0].Name);
	}

	[Test]
	public void CanScaffoldChars()
	{
		var testTable = _databaseModel.Tables.Where(t => t.Name == "TEST").First();	
		Assert.NotNull(testTable);

		var charColumn = testTable.Columns.Where(c => c.Name == "CHAR_FIELD").First();
		Assert.AreEqual("CHAR(30)", charColumn.StoreType);

		var varcharColumn = testTable.Columns.Where(c => c.Name == "VARCHAR_FIELD").First();
		Assert.AreEqual("VARCHAR(100)", varcharColumn.StoreType);

		var csColumn = testTable.Columns.Where(c => c.Name == "CS_FIELD").First();
		Assert.AreEqual("CHAR(1)", csColumn.StoreType);
	}

	[Test]
	public void CanScaffoldDecimalWithPrecisionAndScale()
	{
		var testTable = _databaseModel.Tables.Where(t => t.Name.Equals("TEST")).First();
		Assert.NotNull(testTable);

		var numericColumn = testTable.Columns.Where(c => c.Name == "NUMERIC_FIELD").First();
		Assert.AreEqual("NUMERIC(15,2)", numericColumn.StoreType);

		var decimalColumn = testTable.Columns.Where(c => c.Name == "DECIMAL_FIELD").First();
		Assert.AreEqual("DECIMAL(15,2)", decimalColumn.StoreType);
	}

	private static IDatabaseModelFactory GetModelFactory()
	{
		return new FbDatabaseModelFactory();
	}
}
