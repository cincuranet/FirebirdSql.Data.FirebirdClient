using System;
using System.Linq;
using FirebirdSql.Data.FirebirdClient;
using NUnit.Framework;

namespace FirebirdSql.Data.UnitTests
{
    [FbServerTypeTestFixture(FbServerType.Default, Description = "Tests for retrocompatibility with old boolean support in v2.5")]
	[FbServerTypeTestFixture(FbServerType.Embedded, Description = "Tests for retrocompatibility with old boolean support in v2.5")]
	public class FbBooleanSupportV2_5Tests : TestsBase
	{
        private int _newIdValue;
		private bool _shouldTearDown;

		public FbBooleanSupportV2_5Tests(FbServerType serverType)
			: base(serverType, false)
		{
            _newIdValue = 0;
            _shouldTearDown = false;
		}

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();

			if (!EnsureVersion(currentVersion => currentVersion < new Version("3.0.0.0")))
				return;

			_shouldTearDown = true;
			using (var cmd = Connection.CreateCommand())
			{
				cmd.CommandText = "CREATE TABLE withboolean (id INTEGER, bool Smallint)";
				cmd.ExecuteNonQuery();
			}

			foreach (var value in Enumerable.Range(0, 2))
			{
				using (var cmd = Connection.CreateCommand())
				{
					cmd.CommandText = $"INSERT INTO withboolean (id, bool) VALUES ({_newIdValue++}, 0)";
					cmd.ExecuteNonQuery();
				}
			}
		}

		[TearDown]
		public override void TearDown()
		{
			if (_shouldTearDown)
			{
				using (var cmd = Connection.CreateCommand())
				{
					cmd.CommandText = "DROP TABLE withboolean";
					cmd.ExecuteNonQuery();
				}
			}
			base.TearDown();
		}

		[Test]
		public void ReaderConversionTest()
		{
			using (var cmd = Connection.CreateCommand())
			{
				cmd.CommandText = "SELECT id, bool FROM withboolean";
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						switch (reader.GetInt32(0))
						{
							case 0:
								Assert.IsFalse(reader.GetBoolean(1), "Column with value 0 should have value false.");
								break;
							case 1:
								Assert.IsTrue(reader.GetBoolean(1), "Column with value 1 should have value true.");
								break;
							default:
								Assert.Fail("Unexpected row in result set.");
								break;
						}
					}
				}
			}
		}

        [TestCase(true)]
        [TestCase(false)]
        public void ParametrizedSelectTest(bool implicitParamType)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "SELECT id FROM withboolean where bool = @value";
                if (implicitParamType)
                {
                    cmd.Parameters.Add("@value", true);
                }
                else
                {
                    cmd.Parameters.Add("@value", FbDbType.Boolean);
                    cmd.Parameters[0].Value = true;
                }
                Assert.DoesNotThrow(() => cmd.ExecuteScalar());
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ParametrizedInsertTest(bool implicitParamType)
        {
            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO withboolean (id, bool) VALUES (@id, @bool)";
                cmd.Parameters.Add("id", _newIdValue++);
                if (implicitParamType)
                {
                    cmd.Parameters.Add("bool", true);
                }
                else
                {
                    cmd.Parameters.Add("bool", FbDbType.Boolean);
                    cmd.Parameters[1].Value = true;
                }
                Assert.DoesNotThrow(() => cmd.ExecuteNonQuery());
            }
        }
	}
}
