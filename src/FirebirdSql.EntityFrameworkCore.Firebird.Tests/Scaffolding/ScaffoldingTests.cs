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
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Services;
using FirebirdSql.EntityFrameworkCore.Firebird.Metadata;
using FirebirdSql.EntityFrameworkCore.Firebird.Metadata.Internal;
using FirebirdSql.EntityFrameworkCore.Firebird.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using NUnit.Framework;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Tests.Scaffolding;
#pragma warning disable EF1001
public class ScaffoldingTests : EntityFrameworkCoreTestsBase
{
	private DatabaseModel _databaseModel;
	private Version _serverVersion;

	public override async Task SetUp()
	{
		await base.SetUp();

		_serverVersion = FbServerProperties.ParseServerVersion(Connection.ServerVersion);

		await CreateScaffoldingObjectsAsync();

		var modelFactory = GetModelFactory();
		_databaseModel = modelFactory.Create(Connection, new DatabaseModelFactoryOptions());
	}

	[Test]
	public void JustCanRun()
	{
		var modelFactory = GetModelFactory();
		Assert.DoesNotThrow(() => modelFactory.Create(Connection, new DatabaseModelFactoryOptions()));
	}

	[Test]
	public async Task ReadsNullableCorrect()
	{
		var tableName = "TEST_READS_IS_NULL_FROM_DOMAIN";
		var columnNameNoDomainNull = "NO_DOMAIN_NULL";
		var columnNameNoDomainNotNull = "NO_DOMAIN_NOT_NULL";
		var columnNameDomainNull = "DOMAIN_NULL";
		var columnNameDomainNotNull = "DOMAIN_NOT_NUL";

		using var commandDomainNull = Connection.CreateCommand();
		commandDomainNull.CommandText = "create domain DOMAIN_NULL as INTEGER";
		await commandDomainNull.ExecuteNonQueryAsync();

		using var commandDomainNotNull = Connection.CreateCommand();
		commandDomainNotNull.CommandText = "create domain DOMAIN_NOT_NULL as INTEGER NOT NULL";
		await commandDomainNotNull.ExecuteNonQueryAsync();

		using var commandTable = Connection.CreateCommand();
		commandTable.CommandText = $"create table {tableName} ({columnNameNoDomainNull} INTEGER, {columnNameNoDomainNotNull} INTEGER NOT NULL, {columnNameDomainNull} DOMAIN_NULL, {columnNameDomainNotNull} DOMAIN_NOT_NULL)";
		await commandTable.ExecuteNonQueryAsync();

		var modelFactory = GetModelFactory();
		var model = modelFactory.Create(Connection.ConnectionString, new DatabaseModelFactoryOptions(new string[] { tableName }));
		var table = model.Tables.Single(x => x.Name == tableName);
		var columnNoDomainNull = table.Columns.Single(x => x.Name == columnNameNoDomainNull);
		var columnNoDomainNotNull = table.Columns.Single(x => x.Name == columnNameNoDomainNotNull);
		var columnDomainNull = table.Columns.Single(x => x.Name == columnNameDomainNull);
		var columnDomainNotNull = table.Columns.Single(x => x.Name == columnNameDomainNotNull);

		Assert.Multiple(() =>
		{
			Assert.That(columnNoDomainNull.IsNullable, Is.True);
			Assert.That(columnNoDomainNotNull.IsNullable, Is.False);
			Assert.That(columnDomainNull.IsNullable, Is.True);
			Assert.That(columnDomainNotNull.IsNullable, Is.False);
		});

	}

	[TestCase("SMALLINT")]
	[TestCase("INTEGER")]
	[TestCase("FLOAT")]
	[TestCase("DATE")]
	[TestCase("TIME")]
	[TestCase("CHAR(12)")]
	[TestCase("BIGINT")]
	[TestCase("BOOLEAN")]
	[TestCase("DOUBLE PRECISION")]
	[TestCase("TIMESTAMP")]
	[TestCase("VARCHAR(24)")]
	[TestCase("BLOB SUB_TYPE TEXT")]
	[TestCase("BLOB SUB_TYPE BINARY")]
	[TestCase("DECIMAL(4,1)")]
	[TestCase("DECIMAL(9,1)")]
	[TestCase("DECIMAL(18,1)")]
	[TestCase("NUMERIC(4,1)")]
	[TestCase("NUMERIC(9,1)")]
	[TestCase("NUMERIC(18,1)")]
	public async Task ReadsCorrectFieldType(string dataType)
	{
		var tableName = $"TEST_READS_FIELD_TYPE_CORRECT";
		var columnName = "FIELD";

		using var commandTable = Connection.CreateCommand();
		commandTable.CommandText = $"recreate table {tableName} ({columnName} {dataType})";
		await commandTable.ExecuteNonQueryAsync();

		var modelFactory = GetModelFactory();
		var model = modelFactory.Create(Connection.ConnectionString, new DatabaseModelFactoryOptions(new string[] { tableName }));
		var table = model.Tables.Single(x => x.Name == tableName);
		var column = table.Columns.Single(x => x.Name == columnName);

		Assert.That(column.StoreType, Is.EqualTo(dataType));
	}

	[Test]
	public void CanScaffoldPrimaryKey()
	{
		var testTable = _databaseModel.Tables.Where(t => t.Name.Equals("TEST")).First();

		Assert.NotNull(testTable.PrimaryKey);
		Assert.AreEqual("INT_FIELD", testTable.PrimaryKey.Columns[0].Name);
	}

	[Test]
	public void CanScaffoldGeneratedByIdentities()
	{
		var scaffoldTestTable = _databaseModel.Tables.Where(t => t.Name == "SCAFFOLD_TEST").First();
		Assert.NotNull(scaffoldTestTable);

		var idDefaultColumn = scaffoldTestTable.Columns.Where(c => c.Name == "ID_DEFAULT").First();
		Assert.AreEqual(FbIdentityType.GeneratedByDefault, (FbIdentityType)(idDefaultColumn.GetAnnotation(FbAnnotationNames.IdentityType).Value));
		Assert.IsNull(idDefaultColumn.FindAnnotation(FbAnnotationNames.IdentityStart));  
		Assert.IsNull(idDefaultColumn.FindAnnotation(FbAnnotationNames.IdentityIncrement));

		var idAlwaysColumn = scaffoldTestTable.Columns.Where(c => c.Name == "ID_ALWAYS").First();
		Assert.AreEqual(FbIdentityType.GeneratedAlways, (FbIdentityType)idAlwaysColumn.GetAnnotation(FbAnnotationNames.IdentityType).Value);
		Assert.AreEqual(2, Convert.ToInt32(idAlwaysColumn.GetAnnotation(FbAnnotationNames.IdentityStart).Value));
		Assert.AreEqual(3, Convert.ToInt32(idAlwaysColumn.GetAnnotation(FbAnnotationNames.IdentityIncrement).Value));
	}

	[Test]
	public void CanScaffoldColumns()
	{
		var testTable = _databaseModel.Tables.Where(t => t.Name == "TEST").First();	
		Assert.NotNull(testTable);

		var intColumn = testTable.Columns.Where(c => c.Name == "INT_FIELD").First();
		Assert.AreEqual("INTEGER", intColumn.StoreType);
		Assert.AreEqual("0", intColumn.DefaultValueSql);
		Assert.IsNull(intColumn.FindAnnotation(FbAnnotationNames.IdentityType));

		var charColumn = testTable.Columns.Where(c => c.Name == "CHAR_FIELD").First();
		Assert.AreEqual("CHAR(30)", charColumn.StoreType);

		var varcharColumn = testTable.Columns.Where(c => c.Name == "VARCHAR_FIELD").First();
		Assert.AreEqual("VARCHAR(100)", varcharColumn.StoreType);

		var numericColumn = testTable.Columns.Where(c => c.Name == "NUMERIC_FIELD").First();
		Assert.AreEqual("NUMERIC(15,2)", numericColumn.StoreType);

		var decimalColumn = testTable.Columns.Where(c => c.Name == "DECIMAL_FIELD").First();
		Assert.AreEqual("DECIMAL(15,2)", decimalColumn.StoreType);

		var blobColumn = testTable.Columns.Where(c => c.Name == "BLOB_FIELD").First();
		Assert.AreEqual("BLOB SUB_TYPE BINARY", blobColumn.StoreType);
		Assert.AreEqual(80, Convert.ToInt32(blobColumn.GetAnnotation(FbAnnotationNames.BlobSegmentSize).Value));

		var clobColumn = testTable.Columns.Where(c => c.Name == "CLOB_FIELD").First();
		Assert.AreEqual("BLOB SUB_TYPE TEXT", clobColumn.StoreType);
		Assert.AreEqual(80, Convert.ToInt32(clobColumn.GetAnnotation(FbAnnotationNames.BlobSegmentSize).Value));

		var exprColumn = testTable.Columns.Where(c => c.Name == "EXPR_FIELD").First();
		Assert.AreEqual("(smallint_field * 1000)", exprColumn.ComputedColumnSql);

		var csColumn = testTable.Columns.Where(c => c.Name == "CS_FIELD").First();
		Assert.AreEqual("CHAR(1)", csColumn.StoreType);
		Assert.AreEqual("UNICODE_FSS", csColumn.Collation);
		Assert.AreEqual("UNICODE_FSS", csColumn.GetAnnotation(FbAnnotationNames.CharacterSet).Value.ToString());
	}

	private async Task CreateScaffoldingObjectsAsync()
	{
		await ExecuteDdlAsync(Connection, "DROP TABLE SCAFFOLD_NEW_FB4_TYPES", true);

		await ExecuteDdlAsync(Connection, "DROP TABLE SCAFFOLD_TEST", true);

		await ExecuteDdlAsync(Connection, @"
			CREATE TABLE SCAFFOLD_TEST (
				ID_DEFAULT INTEGER GENERATED BY DEFAULT AS IDENTITY (START WITH 1 INCREMENT BY 1),
				ID_ALWAYS  INTEGER GENERATED ALWAYS     AS IDENTITY (START WITH 2 INCREMENT BY 3)
			)"
		);

		if (_serverVersion.Major >= 4)
		{
			await ExecuteDdlAsync(Connection, @"
				CREATE TABLE SCAFFOLD_NEW_FB4_TYPES (
					INT128_FIELD INT128,
					DECFLOAT_16_FIELD DECFLOAT(16),
					DECFLOAT_34_FIELD DECFLOAT(34),
					TWTZ_FIELD TIME WITH TIME ZONE,
					TSWTZ_FIELD TIMESTAMP WITH TIME ZONE
				)");
		}
	}

	private static async Task ExecuteDdlAsync(FbConnection connection, string ddlScript, bool ignoreOnError = false)
	{
		try
		{
			await using var command = new FbCommand(ddlScript, connection);
			await command.ExecuteNonQueryAsync();
		}
		catch (Exception)
		{
			if (!ignoreOnError)
				throw;
		}
	}

	private static IDatabaseModelFactory GetModelFactory()
	{
		return new FbDatabaseModelFactory();
	}

	static IDatabaseModelFactory GetModelFactory()
	{
		return new FbDatabaseModelFactory();
	}
}
