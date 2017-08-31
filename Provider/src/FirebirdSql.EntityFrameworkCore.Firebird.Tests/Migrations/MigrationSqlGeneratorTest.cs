using System;
using System.Diagnostics;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.EntityFrameworkCore.Firebird.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.TestUtilities.FakeProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Xunit;
using Microsoft.EntityFrameworkCore.Internal;
using Moq;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Tests.Migrations
{
    public class MigrationSqlGeneratorTest : MigrationSqlGeneratorTestBase
    {
        protected override IMigrationsSqlGenerator SqlGenerator
        {
            get
            {
                // type mapper
                var typeMapper = new FbTypeMapper(new RelationalTypeMapperDependencies());

                // migrationsSqlGeneratorDependencies
                var commandBuilderFactory = new RelationalCommandBuilderFactory(
                    new FakeDiagnosticsLogger<DbLoggerCategory.Database.Command>(),
                    typeMapper);

                var FirebirdOptions = new Mock<IFbOptions>();
                FirebirdOptions.SetupGet(opts => opts.ConnectionSettings).Returns(
                    new FbConnectionSettings(new FbConnectionStringBuilder(), new ServerVersion("2.1")));

                FirebirdOptions
                    .Setup(fn =>
                        fn.GetCreateTable(It.IsAny<ISqlGenerationHelper>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns("s"
                    );

                var migrationsSqlGeneratorDependencies = new MigrationsSqlGeneratorDependencies(
                    commandBuilderFactory,
                    new FbSqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()
                    , FirebirdOptions.Object),
                    typeMapper);

                return new FbMigrationsSqlGenerator(
                    migrationsSqlGeneratorDependencies,
                    FirebirdOptions.Object);

                //var FbOptions = new FirebirdOptions();
                //FirebirdOptions.SetupGet(opts => opts.ConnectionSettings).Returns(
                ///    new FbConnectionSettings(new FbConnectionStringBuilder(), new ServerVersion("2.1")));

                /*FirebirdOptions
                    .Setup(fn =>
                        fn.GetCreateTable(It.IsAny<ISqlGenerationHelper>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns("s"
                    );*/

                //return new FbMigrationsSqlGenerator(
                //    migrationsSqlGeneratorDependencies,
                //    FbOptions);
            }
        }

        private static FakeRelationalConnection CreateConnection(IDbContextOptions options = null)
            => new FakeRelationalConnection(options ?? CreateOptions());

        private static IDbContextOptions CreateOptions(RelationalOptionsExtension optionsExtension = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder();

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
                .AddOrUpdateExtension(optionsExtension
                                      ?? new FakeRelationalOptionsExtension().WithConnectionString("test"));

            return optionsBuilder.Options;
        }

        [Fact]
        public override void AddColumnOperation_with_defaultValue()
        {
            base.AddColumnOperation_with_defaultValue();

            Assert.Equal(
                "ALTER TABLE \"People\" ADD \"Name\" varchar(30) NOT NULL DEFAULT 'John Doe';" + EOL,
                Sql);
        }

        [Fact]
        public override void AddColumnOperation_with_defaultValueSql()
        {
            base.AddColumnOperation_with_defaultValueSql();

            Assert.Equal(
                "ALTER TABLE \"People\" ADD \"Birthday\" timestamp DEFAULT CURRENT_TIMESTAMP;" + EOL,
                Sql);
        }

        [Fact]
        public override void AddColumnOperation_with_computed_column_SQL()
	    {
		    base.AddColumnOperation_with_computed_column_SQL();

		    Assert.Equal(
			    "ALTER TABLE \"PEOPLE\" ADD \"BIRTHDAY\" TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;" + EOL,
			    Sql.ToUpper());
	    }

        [Fact]
        public override void AddDefaultDatetimeOperation_with_valueOnUpdate()
        {
            base.AddDefaultDatetimeOperation_with_valueOnUpdate();

            Assert.Equal(
                "ALTER TABLE \"People\" ADD \"Birthday\" timestamp DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;" + EOL,
                Sql);
        }

        [Fact]
        public override void AddDefaultBooleanOperation()
        {
            base.AddDefaultBooleanOperation();

            Assert.Equal(
                "ALTER TABLE \"PEOPLE\" ADD \"ISLEADER\" bit DEFAULT TRUE;".ToUpper() + EOL,
                Sql.ToUpper());
        }

        public override void AddColumnOperation_without_column_type()
        {
            base.AddColumnOperation_without_column_type();

            Assert.Equal(
                "ALTER TABLE \"People\" ADD \"Alias\" text NOT NULL;" + EOL,
                Sql);
        }

        public override void AddColumnOperation_with_maxLength()
        {
            base.AddColumnOperation_with_maxLength();

            Assert.Equal(
                "ALTER TABLE \"Person\" ADD \"Name\" VARCHAR(30);" + EOL,
                Sql);
        }

        public override void AddForeignKeyOperation_with_name()
        {
            base.AddForeignKeyOperation_with_name();

            Assert.Equal(
                "ALTER TABLE \"PEOPLE\" ADD CONSTRAINT \"FK_PEOPLE_COMPANIES\" FOREIGN KEY (\"EMPLOYERID1\", \"EMPLOYERID2\") REFERENCES \"HR\".\"COMPANIES\" (\"ID1\", \"ID2\") ON DELETE CASCADE;" + EOL,
                Sql.ToUpper());
        }

        public override void AddForeignKeyOperation_without_name()
        {
            base.AddForeignKeyOperation_without_name();

            Assert.Equal(
                "ALTER TABLE \"PEOPLE\" ADD FOREIGN KEY (\"SPOUSEID\") REFERENCES \"PEOPLE\" (\"ID\");" + EOL,
                Sql.ToUpper());
        }

        public override void AddPrimaryKeyOperation_with_name()
        {
            base.AddPrimaryKeyOperation_with_name();

            Assert.Equal(
                "ALTER TABLE \"PEOPLE\" ADD CONSTRAINT \"PK_PEOPLE\" PRIMARY KEY (\"ID1\", \"ID2\");" + EOL,
                Sql.ToUpper());
        }

        public override void AddPrimaryKeyOperation_without_name()
        {
            base.AddPrimaryKeyOperation_without_name();
            
            Assert.Equal("ALTER TABLE \"People\" ADD PRIMARY KEY (\"Id\");" + EOL, Sql);
        }

        public override void AddUniqueConstraintOperation_with_name()
        {
            base.AddUniqueConstraintOperation_with_name();

            Assert.Equal(
                "ALTER TABLE \"PEOPLE\" ADD CONSTRAINT \"AK_PEOPLE_DRIVERLICENSE\" UNIQUE (\"DRIVERLICENSE_STATE\", \"DRIVERLICENSE_NUMBER\");" + EOL,
                Sql.ToUpper());
        }

        public override void AddUniqueConstraintOperation_without_name()
        {
            base.AddUniqueConstraintOperation_without_name();

            Assert.Equal(
                "ALTER TABLE \"PEOPLE\" ADD UNIQUE (\"SSN\");" + EOL,
                Sql.ToUpper());
        }

        public override void CreateIndexOperation_unique()
        {
            base.CreateIndexOperation_unique();

            Assert.Equal(
				"CREATE UNIQUE INDEX \"IX_People_Name\" ON \"People\" (\"FirstName\", \"LastName\");" + EOL,
                Sql);
        }
        
        public override void CreateIndexOperation_nonunique()
        {
            base.CreateIndexOperation_nonunique();

            Assert.Equal(
				"CREATE INDEX \"IX_People_Name\" ON \"People\" (\"Name\");" + EOL,
                Sql);
        }

		/*
		// Can't execute this test without generating the index ddl to recreate it properly.
		public override void RenameIndexOperation_works()
		{			
			base.RenameIndexOperation_works();
			Assert.Equal("ALTER TABLE \"People\" DROP INDEX \"IX_People_Discriminator\";" + EOL
			           + "CREATE INDEX \"IX_People_DiscriminatorNew\" ON \"People\" (\"Discriminator\");" + EOL,
				Sql, false, true, true);
		}
		*/

		public virtual void CreateDatabaseOperation()
        {
            Generate(new FbCreateDatabaseOperation { Name = "Northwind" });

            Assert.Equal(
				"CREATE DATABASE \"Northwind\";" + EOL, Sql);
        }

        public override void CreateTableOperation()
        {
            base.CreateTableOperation();

            Assert.Equal(
                "CREATE TABLE \"People\" (" + EOL +
                "    \"Id\" integer NOT NULL," + EOL +
				"    \"EmployerId\" integer," + EOL +
                "    \"SSN\" varchar(11)," + EOL +
                "    PRIMARY KEY (\"Id\")," + EOL +
                "    UNIQUE (\"SSN\")," + EOL +
				"    FOREIGN KEY (\"EmployerId\") REFERENCES \"Companies\" (\"Id\")" + EOL +
                ");" + EOL,
                Sql, false, true, true);
        }

        public override void CreateTableUlongAi()
        {
            base.CreateTableUlongAi();

            Assert.Equal(
                "CREATE TABLE \"TestUlongAutoIncrement\" ("+ EOL +
                "    \"Id\" bigint NOT NULL," + EOL +
				"    PRIMARY KEY (\"Id\")" + EOL +
				");" + EOL +
				"CREATE GENERATOR \"TestUlongAutoIncrement_Id\";" + EOL +
				"CREATE OR ALTER TRIGGER \"TestUlongAutoIncrement_Id\" FOR \"TestUlongAutoIncrement\"" + EOL +
				"ACTIVE BEFORE INSERT POSITION 0 AS BEGIN" + EOL +
				"    IF(new.\"Id\" IS NULL) THEN new.\"Id\" = GEN_ID(\"TestUlongAutoIncrement_Id\",1);" + EOL +
				"END;" + EOL,
				Sql,
                false, true, true);
        }

        public override void DropColumnOperation()
        {
            base.DropColumnOperation();

            Assert.Equal(
                "ALTER TABLE \"PEOPLE\" DROP \"LUCKYNUMBER\";",
                Sql.ToUpper());
        }

        public override void DropForeignKeyOperation()
        {
            base.DropForeignKeyOperation();

            Assert.Equal(
                "ALTER TABLE \"People\" DROP CONSTRAINT \"FK_People_Companies\";" + EOL,
                Sql);
        }

        public override void DropPrimaryKeyOperation()
        {
            base.DropPrimaryKeyOperation();

            Assert.Equal("ALTER TABLE \"People\" DROP CONSTRAINT PK_People",
                Sql);
        }

        public override void DropTableOperation()
        {
            base.DropTableOperation();

            Assert.Equal(
                "DROP TABLE \"People\";" + EOL,
                Sql);
        }

        public override void DropUniqueConstraintOperation()
        {
            base.DropUniqueConstraintOperation();

            Assert.Equal(
                "ALTER TABLE \"PEOPLE\" DROP CONSTRAINT \"AK_PEOPLE_SSN\";" + EOL,
                Sql.ToUpper());
        }

        public override void SqlOperation()
        {
            base.SqlOperation();

            Assert.Equal(
                "-- I <3 DDL" + EOL,
                Sql);
        }

        #region AlterColumn

        public override void AlterColumnOperation()
        {
            base.AlterColumnOperation();
            Assert.Equal(
				"ALTER TABLE \"People\" ALTER COLUMN \"LuckyNumber\" TYPE INTEGER NOT NULL;" + EOL +
				"ALTER TABLE \"People\" ALTER COLUMN \"LuckyNumber\" SET DEFAULT 7" + EOL,
            Sql, false, true, true);
        }

        public override void AlterColumnOperation_without_column_type()
        {
            base.AlterColumnOperation_without_column_type();
            Assert.Equal(
                "ALTER TABLE \"People\" ALTER COLUMN \"LuckyNumber\" TYPE INTEGER NOT NULL;" + EOL +
				"ALTER TABLE \"People\" ALTER COLUMN \"LuckyNumber\" DROP DEFAULT;",
            Sql);
        }

        [Fact]
        public void AlterColumnOperation_dbgenerated_uuid()
        {
            Generate(
                new AlterColumnOperation
                {
                    Table = "People",
                    Name = "GuidKey",
                    ClrType = typeof(int),
                    ColumnType = "char(38)",
                    IsNullable = false,
                    [FbAnnotationNames.ValueGenerationStrategy] = FbValueGenerationStrategy.IdentityColumn
                });

            Assert.Equal(
				"ALTER TABLE \"People\" ALTER COLUMN \"GuidKey\" TYPE CHAR(38) NOT NULL;" + EOL +
				"ALTER TABLE \"People\" ALTER COLUMN \"GuidKey\" DROP DEFAULT;",
            Sql, false , true, true);
        }

        [Theory]
        [InlineData("BLOB TYPE 1")]
        [InlineData("BLOB TYPE 0")]
        public void AlterColumnOperation_with_no_default_value_column_types(string type)
        {
            Generate(
                new AlterColumnOperation
                {
                    Table = "People",
                    Name = "Blob",
                    ClrType = typeof(string),
                    ColumnType = type,
                    IsNullable = true,
                });

            Assert.Equal(
                $"ALTER TABLE \"People\" ALTER COLUMN \"Blob\" TYPE {type};" + EOL,
                Sql, false, true, true);
        }

		#endregion

		[Fact]
		public void AddColumnOperation_with_datetime()
		{
			Generate(new AddColumnOperation
			{
				Table = "People",
				Name = "Birthday",
				ClrType = typeof(DateTime),
				ColumnType = "datetime",
				IsNullable = false,
				DefaultValue = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
			});

			Assert.Equal("ALTER TABLE \"People\" ADD \"Birthday\" datetime NOT NULL DEFAULT '0001-01-01';" + EOL,
				Sql);
		}

		/*
	    protected override IMigrationsSqlGenerator SqlGenerator
	    {
		    get
		    {
			    var FirebirdOptions = new Mock<IFbOptions>();
			    FirebirdOptions.SetupGet(opts => opts.ConnectionSettings).Returns(
				    new FbConnectionSettings(new FbConnectionStringBuilder(), new ServerVersion("2.1")));
			    FirebirdOptions
				    .Setup(fn =>
					    fn.GetCreateTable(It.IsAny<ISqlGenerationHelper>(), It.IsAny<string>(), It.IsAny<string>()))
				    .Returns(
					    "CREATE TABLE \"People\" (" +
					    " \"Id\" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY," +
					    " \"Discriminator\" varchar(63) NOT NULL," +
					    " \"FamilyId\" integer DEFAULT NULL," +
					    " \"Name\" varchar(4000)," +
					    " \"TeacherId\" integer DEFAULT NULL," +
					    " \"Grade\" integer DEFAULT NULL," +
					    " \"Occupation\" varchar(4000)," +
					    " \"OnPta\" smallint DEFAULT NULL," +
					    " PRIMARY KEY (\"Id\")," +
					    " KEY \"IX_People_FamilyId\" (\"FamilyId\")," +
					    " KEY \"IX_People_Discriminator\" (\"Discriminator\")," +
					    " KEY \"IX_People_TeacherId\" (\"TeacherId\")," +
					    " CONSTRAINT \"FK_People_PeopleFamilies_FamilyId\" FOREIGN KEY (\"FamilyId\") REFERENCES \"PeopleFamilies\" (\"Id\") ON DELETE NO ACTION," +
					    " CONSTRAINT \"FK_People_People_TeacherId\" FOREIGN KEY (\"TeacherId\") REFERENCES \"People\" (\"Id\") ON DELETE NO ACTION" +
					    " )");

			    // type mapper
			    var typeMapper = new FbTypeMapper(new RelationalTypeMapperDependencies());

			    // migrationsSqlGeneratorDependencies
			    var commandBuilderFactory = new RelationalCommandBuilderFactory(
				    new FakeDiagnosticsLogger<DbLoggerCategory.Database.Command>(),
				    typeMapper);
			    var migrationsSqlGeneratorDependencies = new MigrationsSqlGeneratorDependencies(
				    commandBuilderFactory,
				    new FbSqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()
					    , FirebirdOptions.Object),
				    typeMapper);

			    return new FbMigrationsSqlGenerator(
				    migrationsSqlGeneratorDependencies,
				    FirebirdOptions.Object);
		    }
	    }

		*/
	}
}
