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
	public class FbServicesTests : TestsBase
	{
		#region Constructors

		public FbServicesTests(FbServerType serverType)
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
		public void BackupRestore_A_BackupTest()
		{
			{
				FbBackup backupSvc = new FbBackup();

				backupSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);
				backupSvc.Options = FbBackupFlags.IgnoreLimbo;
				backupSvc.BackupFiles.Add(new FbBackupFile(ConfigurationManager.AppSettings["BackupRestoreFile"], 2048));
				backupSvc.Verbose = true;

				backupSvc.ServiceOutput += new ServiceOutputEventHandler(ServiceOutput);

				backupSvc.Execute();
			}
			{
				FbRestore restoreSvc = new FbRestore();

				restoreSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);
				restoreSvc.Options = FbRestoreFlags.Create | FbRestoreFlags.Replace;
				restoreSvc.PageSize = 4096;
				restoreSvc.Verbose = true;
				restoreSvc.BackupFiles.Add(new FbBackupFile(ConfigurationManager.AppSettings["BackupRestoreFile"], 2048));

				restoreSvc.ServiceOutput += new ServiceOutputEventHandler(ServiceOutput);

				restoreSvc.Execute();
			}
		}

		[Test]
		public void BackupRestore_A_BackupStreamingTest()
		{
			if (GetServerVersion(this.FbServerType) < new Version("2.5.0.0"))
			{
				Assert.Inconclusive("Not supported on this version.");
				return;
			}

			var ms = new MemoryStream();

			{
				var backupLength = default(long);
				FbStreamingBackup backupSvc = new FbStreamingBackup();
				{
					backupSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);
					backupSvc.Options = FbBackupFlags.IgnoreLimbo;
					backupSvc.OutputStream = ms;

					backupSvc.ServiceOutput += new ServiceOutputEventHandler(ServiceOutput);

					backupSvc.Execute();

					backupLength = ms.Length;
				}
				Assert.Greater(backupLength, 0);
			}

			ms.Position = 0;

			FbStreamingRestore restoreSvc = new FbStreamingRestore();
			{
				restoreSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);
				restoreSvc.Options = FbRestoreFlags.Create | FbRestoreFlags.Replace;
				restoreSvc.PageSize = 4096;
				restoreSvc.Verbose = true;
				restoreSvc.InputStream = ms;

				restoreSvc.ServiceOutput += ServiceOutput;

				restoreSvc.Execute();
			}
		}

		[Test]
		public void ValidationTest()
		{
			FbValidation validationSvc = new FbValidation();

			validationSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);
			validationSvc.Options = FbValidationFlags.ValidateDatabase;

			validationSvc.ServiceOutput += ServiceOutput;

			validationSvc.Execute();
		}

		[Test]
		public void SweepTest()
		{
			FbValidation validationSvc = new FbValidation();

			validationSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);
			validationSvc.Options = FbValidationFlags.SweepDatabase;

			validationSvc.ServiceOutput += ServiceOutput;

			validationSvc.Execute();
		}

		[Test]
		public void SetPropertiesTest()
		{
			FbConfiguration configurationSvc = new FbConfiguration();

			configurationSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);

			configurationSvc.SetSweepInterval(1000);
			configurationSvc.SetReserveSpace(true);
			configurationSvc.SetForcedWrites(true);
		}

		[Test]
		[Category("Local")]
		public void ShutdownOnlineTest()
		{
			FbConfiguration configurationSvc = new FbConfiguration();

			configurationSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);

			configurationSvc.DatabaseShutdown(FbShutdownMode.Forced, 10);
			configurationSvc.DatabaseOnline();
		}

		[Test]
		[Category("Local")]
		public void ShutdownOnline2Test()
		{
			if (GetServerVersion(this.FbServerType) < new Version("2.5.0.0"))
			{
				Assert.Inconclusive("Not supported on this version.");
				return;
			}

			FbConfiguration configurationSvc = new FbConfiguration();

			configurationSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);

			configurationSvc.DatabaseShutdown2(FbShutdownOnlineMode.Full, FbShutdownType.ForceShutdown, 10);
			configurationSvc.DatabaseOnline2(FbShutdownOnlineMode.Normal);
		}

		[Test]
		public void StatisticsTest()
		{
			FbStatistical statisticalSvc = new FbStatistical();

			statisticalSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType);
			statisticalSvc.Options = FbStatisticalFlags.SystemTablesRelations;

			statisticalSvc.ServiceOutput += ServiceOutput;

			statisticalSvc.Execute();
		}

		[Test]
		public void FbLogTest()
		{
			FbLog logSvc = new FbLog();

			logSvc.ConnectionString = BuildServicesConnectionString(this.FbServerType, false);

			logSvc.ServiceOutput += ServiceOutput;

			logSvc.Execute();
		}

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

		[Test]
		public void ServerPropertiesTest()
		{
			FbServerProperties serverProp = new FbServerProperties();

			serverProp.ConnectionString = BuildServicesConnectionString(this.FbServerType, false);

			FbServerConfig serverConfig = serverProp.GetServerConfig();
			FbDatabasesInfo databasesInfo = serverProp.GetDatabasesInfo();

			Console.WriteLine(serverProp.GetMessageFile());
			Console.WriteLine(serverProp.GetLockManager());
			Console.WriteLine(serverProp.GetRootDirectory());
			Console.WriteLine(serverProp.GetImplementation());
			Console.WriteLine(serverProp.GetServerVersion());
			Console.WriteLine(serverProp.GetVersion());
		}

		[Test]
		public void NBackup_A_NBackupTest()
		{
			if (GetServerVersion(this.FbServerType) < new Version("2.5.0.0"))
			{
				Assert.Inconclusive("Not supported on this version.");
				return;
			}

			Action<int> doLevel = l =>
				{
					var nbak = new FbNBackup();

					nbak.ConnectionString = BuildServicesConnectionString(this.FbServerType);
					nbak.Level = l;
					nbak.BackupFile = ConfigurationManager.AppSettings["BackupRestoreFile"] + l.ToString();
					nbak.DirectIO = true;

					nbak.Options = FbNBackupFlags.NoDatabaseTriggers;

					nbak.ServiceOutput += ServiceOutput;

					nbak.Execute();
				};
			doLevel(0);
			doLevel(1);
		}

		[Test]
		public void NBackup_B_NRestoreTest()
		{
			if (GetServerVersion(this.FbServerType) < new Version("2.5.0.0"))
			{
				Assert.Inconclusive("Not supported on this version.");
				return;
			}

			FbConnection.DropDatabase(BuildConnectionString(this.FbServerType));

			var nrest = new FbNRestore();

			nrest.ConnectionString = BuildServicesConnectionString(this.FbServerType);
			nrest.BackupFiles = Enumerable.Range(0, 2).Select(l => ConfigurationManager.AppSettings["BackupRestoreFile"] + l.ToString());
			nrest.DirectIO = true;

			nrest.ServiceOutput += ServiceOutput;

			nrest.Execute();
		}

		#endregion
	}
}
