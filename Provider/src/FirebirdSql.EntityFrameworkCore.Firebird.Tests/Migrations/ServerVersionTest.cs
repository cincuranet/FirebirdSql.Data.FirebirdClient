using System;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Xunit;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Tests.Migrations
{
    public class ServerVersionTest
    {
		 [Theory]
		 [InlineData("2.1", false, 31)]
		 [InlineData("2.5", false, 31)]
		 [InlineData("3.0", true, 64)]
		 [InlineData("4.0", true, 64)]
		 public void TestValidVersion(string version, bool supportIdentityIncrement, int objectLengthName)
		 {
			 var serverVersion = new ServerVersion(version);
			 Assert.Equal(new Version(version), serverVersion.Version);
			 Assert.Equal(supportIdentityIncrement, serverVersion.SupportIdentityIncrement);
			 Assert.Equal(objectLengthName, serverVersion.ObjectLengthName);
		 }

		 [Fact]
		 public void TestInvalidVersion()
		 {
			 Assert.Throws<InvalidOperationException>(() => new ServerVersion("unknown"));
		 }
	}
}
