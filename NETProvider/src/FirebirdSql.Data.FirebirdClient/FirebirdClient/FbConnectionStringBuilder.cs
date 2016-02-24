﻿/*
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
 *  Copyright (c) 2004, 2005 Carlos Guzman Alvarez
 *  All Rights Reserved.
 *
 *  Contributors:
 *		Jiri Cincura (jiri@cincura.net)
 */

using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.ComponentModel;

namespace FirebirdSql.Data.FirebirdClient
{
	public class FbConnectionStringBuilder : DbConnectionStringBuilder
	{
		#region Properties

		[Category("Security")]
		[DisplayName("User ID")]
		[Description("Indicates the User ID to be used when connecting to the data source.")]
		[DefaultValue(FbConnectionString.DefaultUserId)]
		public string UserID
		{
			get { return GetString("User ID"); }
			set { SetValue("User ID", value); }
		}

		[Category("Security")]
		[DisplayName("Password")]
		[Description("Indicates the password to be used when connecting to the data source.")]
		[PasswordPropertyText(true)]
		[DefaultValue(FbConnectionString.DefaultPassword)]
		public string Password
		{
			get { return GetString("Password"); }
			set { SetValue("Password", value); }
		}

		[Category("Source")]
		[DisplayName("DataSource")]
		[Description("The name of the Firebird server to which to connect.")]
		[DefaultValue(FbConnectionString.DefaultDataSource)]
		public string DataSource
		{
			get { return GetString("Data Source"); }
			set { SetValue("Data Source", value); }
		}

		[Category("Source")]
		[DisplayName("Database")]
		[Description("The name of the actual database or the database to be used when a connection is open. It is normally the path to an .FDB file or an alias.")]
		[DefaultValue(FbConnectionString.DefaultCatalog)]
		public string Database
		{
			get { return GetString("Initial Catalog"); }
			set { SetValue("Initial Catalog", value); }
		}

		[Category("Source")]
		[DisplayName("Port")]
		[Description("Port to use for TCP/IP connections")]
		[DefaultValue(FbConnectionString.DefaultPortNumber)]
		public int Port
		{
			get { return GetInt32("Port Number"); }
			set { SetValue("Port Number", value); }
		}

		[Category("Advanced")]
		[DisplayName("PacketSize")]
		[Description("The size (in bytes) of network packets. PacketSize may be in the range 512-32767 bytes.")]
		[DefaultValue(FbConnectionString.DefaultPacketSize)]
		public int PacketSize
		{
			get { return GetInt32("Packet Size"); }
			set { SetValue("Packet Size", value); }
		}

		[Category("Security")]
		[DisplayName("Role")]
		[Description("The user role.")]
		[DefaultValue(FbConnectionString.DefaultRoleName)]
		public string Role
		{
			get { return GetString("Role Name"); }
			set { SetValue("Role Name", value); }
		}

		[Category("Advanced")]
		[DisplayName("Dialect")]
		[Description("The database SQL dialect.")]
		[DefaultValue(FbConnectionString.DefaultDialect)]
		public int Dialect
		{
			get { return GetInt32("Dialect"); }
			set { SetValue("Dialect", value); }
		}

		[Category("Advanced")]
		[DisplayName("Character Set")]
		[Description("The connection character set encoding.")]
		[DefaultValue(FbConnectionString.DefaultCharacterSet)]
		public string Charset
		{
			get { return GetString("Character Set"); }
			set { SetValue("Character Set", value); }
		}

		[Category("Connection")]
		[DisplayName("Connection Timeout")]
		[Description("The time (in seconds) to wait for a connection to open.")]
		[DefaultValue(FbConnectionString.DefaultConnectionTimeout)]
		public int ConnectionTimeout
		{
			get { return GetInt32("Connection Timeout"); }
			set { SetValue("Connection Timeout", value); }
		}

		[Category("Pooling")]
		[DisplayName("Pooling")]
		[Description("When true the connection is grabbed from a pool or, if necessary, created and added to the appropriate pool.")]
		[DefaultValue(FbConnectionString.DefaultPooling)]
		public bool Pooling
		{
			get { return GetBoolean("Pooling"); }
			set { SetValue("Pooling", value); }
		}

		[Category("Connection")]
		[DisplayName("Connection LifeTime")]
		[Description("When a connection is returned to the pool, its creation time is compared with the current time, and the connection is destroyed if that time span (in seconds) exceeds the value specified by connection lifetime.")]
		[DefaultValue(FbConnectionString.DefaultConnectionLifetime)]
		public int ConnectionLifeTime
		{
			get { return GetInt32("Connection Lifetime"); }
			set { SetValue("Connection Lifetime", value); }
		}

		[Category("Pooling")]
		[DisplayName("MinPoolSize")]
		[Description("The minimun number of connections allowed in the pool.")]
		[DefaultValue(FbConnectionString.DefaultMinPoolSize)]
		public int MinPoolSize
		{
			get { return GetInt32("Min Pool Size"); }
			set { SetValue("Min Pool Size", value); }
		}

		[Category("Pooling")]
		[DisplayName("MaxPoolSize")]
		[Description("The maximum number of connections allowed in the pool.")]
		[DefaultValue(FbConnectionString.DefaultMaxPoolSize)]
		public int MaxPoolSize
		{
			get { return GetInt32("Max Pool Size"); }
			set { SetValue("Max Pool Size", value); }
		}

		[Category("Advanced")]
		[DisplayName("FetchSize")]
		[Description("The maximum number of rows to be fetched in a single call to read into the internal row buffer.")]
		[DefaultValue(FbConnectionString.DefaultFetchSize)]
		public int FetchSize
		{
			get { return GetInt32("Fetch Size"); }
			set { SetValue("Fetch Size", value); }
		}

		[Category("Source")]
		[DisplayName("ServerType")]
		[Description("The type of server used.")]
		[DefaultValue(FbConnectionString.DefaultServerType)]
		public FbServerType ServerType
		{
			get { return GetServerType("Server Type"); }
			set { SetValue("Server Type", value); }
		}

		[Category("Advanced")]
		[DisplayName("IsolationLevel")]
		[Description("The default Isolation Level for implicit transactions.")]
		[DefaultValue(FbConnectionString.DefaultIsolationLevel)]
		public IsolationLevel IsolationLevel
		{
			get { return (IsolationLevel)GetInt32("Isolation Level"); }
			set { SetValue("Isolation Level", value); }
		}

		[Category("Advanced")]
		[DisplayName("Records Affected")]
		[Description("Get the number of rows affected by a command when true.")]
		[DefaultValue(FbConnectionString.DefaultRecordsAffected)]
		public bool ReturnRecordsAffected
		{
			get { return GetBoolean("Records Affected"); }
			set { SetValue("Records Affected", value); }
		}

		[Category("Pooling")]
		[DisplayName("Enlist")]
		[Description("If true, enlists the connections in the current transaction.")]
		[DefaultValue(FbConnectionString.DefaultPooling)]
		public bool Enlist
		{
			get { return GetBoolean("Enlist"); }
			set { SetValue("Enlist", value); }
		}

		[Category("Advanced")]
		[DisplayName("Client Library")]
		[Description("Client library for Firebird Embedded Server.")]
		[DefaultValue(FbConnectionString.DefaultClientLibrary)]
		public string ClientLibrary
		{
			get { return GetString("Client Library"); }
			set { SetValue("Client Library", value); }
		}

		[Category("Advanced")]
		[DisplayName("Cache Pages")]
		[Description("How many cache buffers to use for this session.")]
		[DefaultValue(FbConnectionString.DefaultCachePages)]
		public int DbCachePages
		{
			get { return GetInt32("Cache Pages"); }
			set { SetValue("Cache Pages", value); }
		}

		[Category("Advanced")]
		[DisplayName("No Triggers")]
		[Description("Disables database triggers for this connection.")]
		[DefaultValue(FbConnectionString.DefaultNoDbTriggers)]
		public bool NoDatabaseTriggers
		{
			get { return GetBoolean("No DB Triggers"); }
			set { SetValue("No DB Triggers", value); }
		}

        [Category("Advanced")]
        [DisplayName("NoGarbageCollect")]
        [Description("If true, disables sweeping the database upon attachment.")]
        [DefaultValue(FbConnectionString.DefaultNoGarbageCollect)]
        public bool NoGarbageCollect
        {
            get { return GetBoolean("No Garbage Collect"); }
            set { SetValue("No Garbage Collect", value); }
        }

		#endregion

		#region Constructors

		public FbConnectionStringBuilder()
		{
		}

		public FbConnectionStringBuilder(string connectionString)
		{
			ConnectionString = connectionString;
		}

		#endregion

		#region Private methods

		private int GetInt32(string keyword)
		{
			return Convert.ToInt32(this[GetKey(keyword)]);
		}

		private FbServerType GetServerType(string keyword)
		{
			object value = this[GetKey(keyword)];

			if (value is FbServerType)
			{
				return (FbServerType)value;
			}
			else if (value is string)
			{
				switch (value.ToString())
				{
					case "Default":
						return FbServerType.Default;

					case "Embedded":
						return FbServerType.Embedded;
				}
			}

			return (FbServerType)GetInt32(keyword);
		}

		private string GetString(string keyword)
		{
			return Convert.ToString(this[GetKey(keyword)]);
		}

		private bool GetBoolean(string keyword)
		{
			return Convert.ToBoolean(this[GetKey(keyword)]);
		}

		private void SetValue<T>(string keyword, T value)
		{
			this[GetKey(keyword)] = value;
		}

		private string GetKey(string keyword)
		{
			string synonymKey = FbConnectionString.Synonyms[keyword];

			// First check if there are yet a property for the requested keyword
			foreach (string key in Keys)
			{
				if (FbConnectionString.Synonyms.ContainsKey(key) && FbConnectionString.Synonyms[key] == synonymKey)
				{
					synonymKey = key;
					break;
				}
			}

			return synonymKey;
		}

		#endregion
	}
}
