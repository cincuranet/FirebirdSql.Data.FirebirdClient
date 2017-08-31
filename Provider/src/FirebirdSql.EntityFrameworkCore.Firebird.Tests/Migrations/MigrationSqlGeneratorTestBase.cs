using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Linq;
using FirebirdSql.EntityFrameworkCore.Firebird.Tests.TestUtilities;
using Xunit;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Tests.Migrations
{
    public abstract class MigrationSqlGeneratorTestBase
    {
        protected static string EOL => Environment.NewLine;

        protected abstract IMigrationsSqlGenerator SqlGenerator { get; }

        protected virtual string Sql { get; set; }

        [Fact]
        public virtual void AddColumnOperation_with_defaultValue()
        {
            Generate(
                new AddColumnOperation
                {
                    Table = "People",
                    Schema = "", // "dbo",
                    Name = "Name",
                    ClrType = typeof(string),
                    ColumnType = "varchar(30)",
                    IsNullable = false,
                    DefaultValue = "John Doe"
                });
        }

        [Fact]
        public virtual void AddColumnOperation_with_defaultValueSql()
        {
            Generate(
                new AddColumnOperation
                {
                    Table = "People",
                    Name = "Birthday",
                    ClrType = typeof(DateTime),
                    ColumnType = "timestamp",
                    IsNullable = true,
                    [FbAnnotationNames.ValueGenerationStrategy] = FbValueGenerationStrategy.IdentityColumn
                });
        }

        [Fact]
        public virtual void AddColumnOperation_with_computed_column_SQL()
        {
            Generate(
                new AddColumnOperation
                {
                    Table = "People",
                    Name = "Birthday",
                    ClrType = typeof(DateTime),
                    ColumnType = "timestamp",
                    IsNullable = true,
                    [FbAnnotationNames.ValueGenerationStrategy] = FbValueGenerationStrategy.ComputedColumn
                });
        }

        [Fact]
        public virtual void AddDefaultDatetimeOperation_with_valueOnUpdate()
        {
            Generate(
                new AddColumnOperation
                {
                    Table = "People",
                    Name = "Birthday",
                    ClrType = typeof(DateTime),
                    ColumnType = "timestamp",
                    IsNullable = true,
                    [FbAnnotationNames.ValueGenerationStrategy] = FbValueGenerationStrategy.ComputedColumn
                });
        }

        [Fact]
        public virtual void AddDefaultBooleanOperation()
        {
            Generate(
                new AddColumnOperation
                {
                    Table = "People",
                    Name = "IsLeader",
                    ClrType = typeof(Boolean),
                    ColumnType = "bit",
                    IsNullable = true,
                    DefaultValue = true
                });
        }

        [Fact]
        public virtual void AddColumnOperation_without_column_type()
        {
            Generate(
                new AddColumnOperation
                {
                    Table = "People",
                    Name = "Alias",
                    ClrType = typeof(string),
                    ColumnType = "text",
                    IsNullable = false,
                });
        }

        [Fact]
        public virtual void AddForeignKeyOperation_with_name()
        {
            Generate(
                new AddForeignKeyOperation
                {
                    Table = "People",
                    Schema = "", // "dbo",
                    Name = "FK_People_Companies",
                    Columns = new[] { "EmployerId1", "EmployerId2" },
                    PrincipalTable = "Companies",
                    PrincipalSchema = "hr",
                    PrincipalColumns = new[] { "Id1", "Id2" },
                    OnDelete = ReferentialAction.Cascade
                });
        }

        [Fact]
        public virtual void AddForeignKeyOperation_without_name()
        {
            Generate(
                new AddForeignKeyOperation
                {
                    Table = "People",
                    Columns = new[] { "SpouseId" },
                    PrincipalTable = "People",
                    PrincipalColumns = new[] { "Id" }
                });
        }

        [Fact]
        public virtual void AddPrimaryKeyOperation_with_name()
        {
            Generate(
                new AddPrimaryKeyOperation
                {
                    Table = "People",
                    Schema = "", // "dbo",
                    Name = "PK_People",
                    Columns = new[] { "Id1", "Id2" }
                });
        }

        [Fact]
        public virtual void AddPrimaryKeyOperation_without_name()
        {
            Generate(
                new AddPrimaryKeyOperation
                {
                    Table = "People",
                    Columns = new[] { "Id" }
                });
        }

        [Fact]
        public virtual void AddUniqueConstraintOperation_with_name()
        {
            Generate(
                new AddUniqueConstraintOperation
                {
                    Table = "People",
                    Schema = "", // "dbo",
                    Name = "AK_People_DriverLicense",
                    Columns = new[] { "DriverLicense_State", "DriverLicense_Number" }
                });
        }

        [Fact]
        public virtual void AddUniqueConstraintOperation_without_name()
        {
            Generate(
                new AddUniqueConstraintOperation
                {
                    Table = "People",
                    Columns = new[] { "SSN" }
                });
        }

        [Fact]
        public virtual void AlterColumnOperation()
        {
            Generate(
                new AlterColumnOperation
                {
                    Table = "People",
                    Schema = "", // "dbo",
                    Name = "LuckyNumber",
                    ClrType = typeof(int),
                    ColumnType = "integer",
                    IsNullable = false,
                    DefaultValue = 7
                });
        }

        [Fact]
        public virtual void AlterColumnOperation_without_column_type()
        {
            Generate(
                new AlterColumnOperation
                {
                    Table = "People",
                    Name = "LuckyNumber",
                    ClrType = typeof(int),
                    ColumnType = "integer",
                });
        }

        [Fact]
        public virtual void AddColumnOperation_with_maxLength()
        {
            Generate(
                modelBuilder => modelBuilder.Entity("Person").Property<string>("Name").HasMaxLength(30),
                new AddColumnOperation
                {
                    Table = "Person",
                    Name = "Name",
                    ClrType = typeof(string),
                    MaxLength = 30,
                    IsNullable = true
                });
        }

        [Fact]
        public virtual void RenameTableOperation()
        {
            Generate(
                new RenameTableOperation
                {
                    Name = "People",
                    NewName = "Personas"
                });
        }

        [Fact]
        public virtual void CreateIndexOperation_unique()
        {
            Generate(
                new CreateIndexOperation
                {
                    Name = "IX_People_Name",
                    Table = "People",
                    Schema = "", // "dbo",
                    Columns = new[] { "FirstName", "LastName" },
                    IsUnique = true
                });
        }
        
        [Fact]
        public virtual void CreateIndexOperation_nonunique()
        {
            Generate(
                new CreateIndexOperation
                {
                    Name = "IX_People_Name",
                    Table = "People",
                    Columns = new[] { "Name" },
                    IsUnique = false
                });
        }

        /*[Fact]
        public virtual void RenameIndexOperation_works()
        {
            Generate(
                new RenameIndexOperation
                {
                    IsDestructiveChange = false,
                    Name = "IX_People_Discriminator",
                    NewName = "IX_People_DiscriminatorNew",
                    Table = "People"
                });
        }*/
        
        [Fact]
        public virtual void CreateTableOperation()
        {
            Generate(
                new CreateTableOperation
                {
                    Name = "People",
                    Schema = "",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "People",
                            ClrType = typeof(int),
                            ColumnType = "integer",
                            IsNullable = false
                        },
                        new AddColumnOperation
                        {
                            Name = "EmployerId",
                            Table = "People",
                            ClrType = typeof(int),
                            ColumnType = "integer",
                            IsNullable = true
                        },
                        new AddColumnOperation
                        {
                            Name = "SSN",
                            Table = "People",
                            ClrType = typeof(string),
                            ColumnType = "varchar(11)",
                            IsNullable = true
                        }
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    },
                    UniqueConstraints =
                    {
                        new AddUniqueConstraintOperation
                        {
                            Columns = new[] { "SSN" }
                        }
                    },
                    ForeignKeys =
                    {
                        new AddForeignKeyOperation
                        {
                            Columns = new[] { "EmployerId" },
                            PrincipalTable = "Companies",
                            PrincipalColumns = new[] { "Id" }
                        }
                    }
                });
        }

        [Fact]
        public virtual void CreateTableUlongAi()
        {
            Generate(
                new CreateTableOperation
                {
                    Name = "TestUlongAutoIncrement",
                    Schema = "",
                    Columns =
                    {
                        new AddColumnOperation
                        {
                            Name = "Id",
                            Table = "TestUlongAutoIncrement",
                            ClrType = typeof(ulong),
                            ColumnType = "bigint",
                            IsNullable = false,
                            [FbAnnotationNames.ValueGenerationStrategy] = FbValueGenerationStrategy.IdentityColumn
                        }
                    },
                    PrimaryKey = new AddPrimaryKeyOperation
                    {
                        Columns = new[] { "Id" }
                    }
                });
        }

        [Fact]
        public virtual void DropColumnOperation()
        {
            Generate(
                new DropColumnOperation
                {
                    Table = "People",
                    Schema = "",
                    Name = "LuckyNumber"
                });
        }

        [Fact]
        public virtual void DropForeignKeyOperation()
        {
            Generate(
                new DropForeignKeyOperation
                {
                    Table = "People",
                    Schema = "", // "dbo",
                    Name = "FK_People_Companies"
                });
        }

        [Fact]
        public virtual void DropIndexOperation()
        {
            Generate(
                new DropIndexOperation
                {
                    Name = "IX_People_Name",
                    Table = "People",
                    Schema = ""
                });
        }

        [Fact]
        public virtual void DropPrimaryKeyOperation()
        {
            Generate(
                new DropPrimaryKeyOperation
                {
                    Table = "People",
                    Schema = "",
                    Name = "PK_People"
                });
        }

        [Fact]
        public virtual void DropTableOperation()
        {
            Generate(
                new DropTableOperation
                {
                    Name = "People",
                    Schema = ""
                });
        }

        [Fact]
        public virtual void DropUniqueConstraintOperation()
        {
            Generate(
                new DropUniqueConstraintOperation
                {
                    Table = "People",
                    Schema = "", // "dbo",
                    Name = "AK_People_SSN"
                });
        }

        [Fact]
        public virtual void SqlOperation()
        {
            Generate(
                new SqlOperation
                {
                    Sql = "-- I <3 DDL"
                });
        }

        protected virtual void Generate(params MigrationOperation[] operation)
            => Generate(_ => { }, operation);

        protected virtual ModelBuilder CreateModelBuilder() => FbTestHelpers.Instance.CreateConventionBuilder();

        protected virtual void Generate(Action<ModelBuilder> buildAction, params MigrationOperation[] operation)
        {
            var modelBuilder = CreateModelBuilder();
            buildAction(modelBuilder);

            var batch = SqlGenerator.Generate(operation, modelBuilder.Model);

            Sql = string.Join("", batch.Select(b => b.CommandText));
        }
    }
}
