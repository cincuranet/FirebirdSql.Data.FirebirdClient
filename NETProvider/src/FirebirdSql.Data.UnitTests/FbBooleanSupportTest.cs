using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdSql.Data.UnitTests
{
	[TestFixture(FbServerType.Default, EngineVersion.v3_0)]
	[TestFixture(FbServerType.Embedded, EngineVersion.v3_0)]
	public class FbBooleanSupportTest : TestsBase
	{
		private static readonly string s_CreateTable =
			"CREATE TABLE withboolean ( id INTEGER, bool BOOLEAN )";
		private static readonly string s_DropTable =
			"DROP TABLE withboolean";
		private static readonly string s_Insert = "INSERT INTO withboolean (id, bool) VALUES (?, ?)";
		private static readonly string s_Select = "SELECT id, bool FROM withboolean";
		private static readonly string s_SelectConditionBoolField = s_Select + " WHERE bool = ?";
		private static readonly string s_SelectConditionSingleton = s_Select + " WHERE ?";
		private static readonly string[] s_TestData = {
			"INSERT INTO withboolean (id, bool) VALUES (0, FALSE)",
			"INSERT INTO withboolean (id, bool) VALUES (1, TRUE)",
			"INSERT INTO withboolean (id, bool) VALUES (2, UNKNOWN)"};

		private bool setuped = false;

		public FbBooleanSupportTest(FbServerType serverType, EngineVersion version)
			: base(serverType, false, version)
		{
		}

		private void Check30ServerVersion()
		{
			if (GetServerVersion() < new Version("3.0.0.0"))
			{
				Assert.Inconclusive("Not supported on this version.");
				return;
			}

			this.setuped = true;
		}

		[SetUp]
		public override void SetUp()
		{
			this.Check30ServerVersion();
			base.SetUp();
			using (FbCommand cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_CreateTable;
				cmd.ExecuteNonQuery();
			}
			foreach (var q in s_TestData)
			{
				using (FbCommand cmd = this.Connection.CreateCommand())
				{
					cmd.CommandText = q;
					cmd.ExecuteNonQuery();
				}
			}
		}

		[TearDown]
		public override void TearDown()
		{
			if (setuped)
			{
				using (FbCommand cmd = this.Connection.CreateCommand())
				{
					cmd.CommandText = s_DropTable;
					cmd.ExecuteNonQuery();
				}

				base.TearDown();
			}
		}

		[Test]
		public void testSimpleSelect_Values()
		{
			using (FbCommand cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_Select;
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						int id = reader.GetInt32(0);
						switch (id)
						{
							case 0:
								Assert.False(reader.GetBoolean(1), "Column with value FALSE should have value false");
								Assert.False(reader.IsDBNull(1), "Column with value FALSE should not be null");
								break;
							case 1:
								Assert.True(reader.GetBoolean(1), "Column with value TRUE should have value true");
								Assert.False(reader.IsDBNull(1), "Column with value TRUE should not be null");
								break;
							case 2:
								Assert.True(reader.IsDBNull(1), "Column with value UNKNOWN should be null");
								break;
							default:
								Assert.Fail("Unexpected row in result set");
								break;
						}
					}
				}
			}
		}

		[Test]
		public void testSimpleSelect_ResultSetMetaData()
		{
			using (FbCommand cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_Select;
				using (var reader = cmd.ExecuteReader())
				{
					var dataTable = reader.GetSchemaTable();
					//Assert.AreEqual(typeof(bool), dataTable.Columns[2].DataType, "Unexpected type for boolean column");
				}
			}
		}

		[Test]
		public void testParametrizedInsert()
		{
			using (var cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_Insert;
				var param = cmd.CreateParameter();
				param.Value = 3;
				cmd.Parameters.Add(param);
				param = cmd.CreateParameter();
				param.Value = false;
				cmd.Parameters.Add(param);
				cmd.ExecuteNonQuery();
			}
			using (var cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_Insert;
				var param = cmd.CreateParameter();
				param.Value = 4;
				cmd.Parameters.Add(param);
				param = cmd.CreateParameter();
				param.Value = true;
				cmd.Parameters.Add(param);
				cmd.ExecuteNonQuery();
			}
			using (var cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_Insert;
				var param = cmd.CreateParameter();
				param.Value = 5;
				cmd.Parameters.Add(param);
				param = cmd.CreateParameter();
				param.Value = null;
				cmd.Parameters.Add(param);
				cmd.ExecuteNonQuery();
			}
			using (FbCommand cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_Select;
				using (var reader = cmd.ExecuteReader())
				{
					int count = 0;
					while (reader.Read())
					{
						++count;
						int id = reader.GetInt32(0);
						switch (id)
						{
							case 0:
							case 1:
							case 2:
								continue;
							case 3:
								Assert.False(reader.GetBoolean(1), "Column with value FALSE should have value false");
								Assert.False(reader.IsDBNull(1), "Column with value FALSE should not be null");
								break;
							case 4:
								Assert.True(reader.GetBoolean(1), "Column with value TRUE should have value true");
								Assert.False(reader.IsDBNull(1), "Column with value TRUE should not be null");
								break;
							case 5:
								Assert.True(reader.IsDBNull(1), "Column with value UNKNOWN should be null");
								break;
							default:
								Assert.Fail("Unexpected row in result set");
								break;
						}
					}
					Assert.AreEqual(6, count);
				}
			}
		}

		[Test]
		public void testParametrizedInsert_ParameterMetaData()
		{
			using (var cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_Select;
				cmd.Prepare();
				using (var reader = cmd.ExecuteReader())
				{
					var dt = reader.GetSchemaTable();
					Assert.AreEqual("BOOL", dt.Rows[1].ItemArray[0]);
					Assert.AreEqual(typeof(bool), dt.Rows[1].ItemArray[5]);
				}
			}
		}

		[Test]
		public void testSelectFieldCondition()
		{
			using (var cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_SelectConditionBoolField;
				var param = cmd.CreateParameter();
				param.Value = true;
				cmd.Parameters.Add(param);
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read(), "Expected a row");
					Assert.AreEqual(1, reader.GetInt32(0), "Expected row with id=1");
					Assert.False(reader.Read(), "Did not expect a second row");
				}
			}
		}

		[Test]
		public void testSelect_ConditionOnly_true()
		{
			using (var cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_SelectConditionSingleton;
				var param = cmd.CreateParameter();
				param.Value = true;
				cmd.Parameters.Add(param);
				using (var reader = cmd.ExecuteReader())
				{
					int count = 0;
					while (reader.Read())
					{
						++count;
						int id = reader.GetInt32(0);
						switch (id)
						{
							case 0:
								Assert.False(reader.GetBoolean(1), "Column with value FALSE should have value false");
								Assert.False(reader.IsDBNull(1), "Column with value FALSE should not be null");
								break;
							case 1:
								Assert.True(reader.GetBoolean(1), "Column with value TRUE should have value true");
								Assert.False(reader.IsDBNull(1), "Column with value TRUE should not be null");
								break;
							case 2:
								Assert.True(reader.IsDBNull(1), "Column with value UNKNOWN should be null");
								break;
							default:
								Assert.Fail("Unexpected row in result set");
								break;
						}
					}

					Assert.AreEqual(3, count);
				}
			}
		}

		[Test]
		public void testSelect_ConditionOnly_false()
		{
			using (var cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_SelectConditionSingleton;
				var param = cmd.CreateParameter();
				param.Value = false;
				cmd.Parameters.Add(param);
				using (var reader = cmd.ExecuteReader())
				{
					Assert.False(reader.Read());
				}
			}
		}

		[Test]
		public void testSelect_ConditionOnly_null()
		{
			using (var cmd = this.Connection.CreateCommand())
			{
				cmd.CommandText = s_SelectConditionSingleton;
				var param = cmd.CreateParameter();
				param.Value = null;
				cmd.Parameters.Add(param);
				using (var reader = cmd.ExecuteReader())
				{
					Assert.False(reader.Read());
				}
			}
		}
	}
}
