using FirebirdSql.Data.FirebirdClient;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace FirebirdSql.Data.UnitTests
{
	[TestFixture()]
	public class FbConnectionPoolManagerTest
	{
		[Test]
		public void IsAliveTrueIfLifetimeNotExceed()
		{
			var timeAgo = (FbConnectionPoolManager.Pool.GetTicks() - (10 * 1000)); //10 seconds
			var now = FbConnectionPoolManager.Pool.GetTicks();
			var isAlive = FbConnectionPoolManager.Pool.IsAlive(20, timeAgo, now);
			Assert.IsTrue(isAlive);
		}

		[Test]
		public void IsAliveFalseIfLifetimeIsExceed()
		{
			var timeAgo = (FbConnectionPoolManager.Pool.GetTicks() - (30 * 1000)); //30 seconds
			var now = FbConnectionPoolManager.Pool.GetTicks();
			var isAlive = FbConnectionPoolManager.Pool.IsAlive(20, timeAgo, now);
			Assert.IsFalse(isAlive);
		}
	}
}