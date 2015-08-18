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
 *  
 *  Contributors:
 *   Jiri Cincura (jiri@cincura.net)   
 */

using System;
using System.Configuration;
using System.IO;
using System.Data;
using System.Text;
using System.Linq;

using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Services;
using NUnit.Framework;

namespace FirebirdSql.Data.UnitTests
{
	[TestFixture(FbServerType.Default)]
	public class FbUserServicesTests : TestsBase
	{
		#region Constructors

		public FbUserServicesTests(FbServerType serverType)
			: base(serverType, false)
		{
		}

		#endregion

		#region Setup Method

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();

			if (this.Connection != null && this.Connection.State == ConnectionState.Open)
			{
				this.Connection.Close();
			}
		}

		#endregion

		#region Event Handlers

		void ServiceOutput(object sender, ServiceOutputEventArgs e)
		{
			Console.WriteLine(e.Message);
		}

		#endregion

		#region Unit Tests
        
		[Test]
		public void AddUserTest()
		{
			FbSecurity securitySvc = new FbSecurity();

			securitySvc.ConnectionString = BuildServicesConnectionString(this.FbServerType, false);

			FbUserData user = new FbUserData();

			user.UserName = "new_user";
			user.UserPassword = "1";

			securitySvc.AddUser(user);
		}

		[Test]
		public void DeleteUser()
		{
			FbSecurity securitySvc = new FbSecurity();

			securitySvc.ConnectionString = BuildServicesConnectionString(this.FbServerType, false);

			FbUserData user = new FbUserData();

			user.UserName = "new_user";

			securitySvc.DeleteUser(user);
		}

		[Test]
		public void DisplayUser()
		{
			FbSecurity securitySvc = new FbSecurity();

			securitySvc.ConnectionString = BuildServicesConnectionString(this.FbServerType, false);

			FbUserData user = securitySvc.DisplayUser("SYSDBA");

			Console.WriteLine("User name {0}", user.UserName);
		}

		[Test]
		public void DisplayUsers()
		{
			FbSecurity securitySvc = new FbSecurity();

			securitySvc.ConnectionString = BuildServicesConnectionString(this.FbServerType, false);

			FbUserData[] users = securitySvc.DisplayUsers();

			Console.WriteLine("User List");

			for (int i = 0; i < users.Length; i++)
			{
				Console.WriteLine("User {0} name {1}", i, users[i].UserName);
			}
		}
        
		#endregion
	}
}
