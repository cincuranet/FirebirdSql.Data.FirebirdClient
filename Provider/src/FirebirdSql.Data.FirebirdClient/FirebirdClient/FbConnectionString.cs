﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/blob/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$Authors = Carlos Guzman Alvarez, Jiri Cincura (jiri@cincura.net)

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.FirebirdClient
{
	internal sealed class FbConnectionString
	{
		#region Constants
		internal const string DefaultValueDataSource = "";
		internal const int DefaultValuePortNumber = 3050;
		internal const string DefaultValueUserId = "";
		internal const string DefaultValuePassword = "";
		internal const string DefaultValueRoleName = "";
		internal const string DefaultValueCatalog = "";
		internal const string DefaultValueCharacterSet = "NONE";
		internal const int DefaultValueDialect = 3;
		internal const int DefaultValuePacketSize = 8192;
		internal const bool DefaultValuePooling = true;
		internal const int DefaultValueConnectionLifetime = 0;
		internal const int DefaultValueMinPoolSize = 0;
		internal const int DefaultValueMaxPoolSize = 100;
		internal const int DefaultValueConnectionTimeout = 15;
		internal const int DefaultValueFetchSize = 200;
		internal const FbServerType DefaultValueServerType = FbServerType.Default;
		internal const IsolationLevel DefaultValueIsolationLevel = IsolationLevel.ReadCommitted;
		internal const bool DefaultValueRecordsAffected = true;
		internal const bool DefaultValueEnlist = true;
		internal const string DefaultValueClientLibrary = "fbembed";
		internal const int DefaultValueDbCachePages = 0;
		internal const bool DefaultValueNoDbTriggers = false;
		internal const bool DefaultValueNoGarbageCollect = false;
		internal const bool DefaultValueCompression = false;
		internal const byte[] DefaultValueCryptKey = null;

		internal const string DefaultKeyUserId = "user id";
		internal const string DefaultKeyPortNumber = "port number";
		internal const string DefaultKeyDataSource = "data source";
		internal const string DefaultKeyPassword = "password";
		internal const string DefaultKeyRoleName = "role name";
		internal const string DefaultKeyCatalog = "initial catalog";
		internal const string DefaultKeyCharacterSet = "character set";
		internal const string DefaultKeyDialect = "dialect";
		internal const string DefaultKeyPacketSize = "packet size";
		internal const string DefaultKeyPooling = "pooling";
		internal const string DefaultKeyConnectionLifetime = "connection lifetime";
		internal const string DefaultKeyMinPoolSize = "min pool size";
		internal const string DefaultKeyMaxPoolSize = "max pool size";
		internal const string DefaultKeyConnectionTimeout = "connection timeout";
		internal const string DefaultKeyFetchSize = "fetch size";
		internal const string DefaultKeyServerType = "server type";
		internal const string DefaultKeyIsolationLevel = "isolation level";
		internal const string DefaultKeyRecordsAffected = "records affected";
		internal const string DefaultKeyEnlist = "enlist";
		internal const string DefaultKeyClientLibrary = "client library";
		internal const string DefaultKeyDbCachePages = "cache pages";
		internal const string DefaultKeyNoDbTriggers = "no db triggers";
		internal const string DefaultKeyNoGarbageCollect = "no garbage collect";
		internal const string DefaultKeyCompression = "compression";
		internal const string DefaultKeyCryptKey = "crypt key";
		#endregion

		#region Static Fields

		internal static readonly IDictionary<string, string> Synonyms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ DefaultKeyDataSource, DefaultKeyDataSource },
			{ "datasource", DefaultKeyDataSource },
			{ "server", DefaultKeyDataSource },
			{ "host", DefaultKeyDataSource },
			{ "port", DefaultKeyPortNumber },
			{ DefaultKeyPortNumber, DefaultKeyPortNumber },
			{ "database", DefaultKeyCatalog },
			{ DefaultKeyCatalog, DefaultKeyCatalog },
			{ DefaultKeyUserId, DefaultKeyUserId },
			{ "userid", DefaultKeyUserId },
			{ "uid", DefaultKeyUserId },
			{ "user", DefaultKeyUserId },
			{ "user name", DefaultKeyUserId },
			{ "username", DefaultKeyUserId },
			{ DefaultKeyPassword, DefaultKeyPassword },
			{ "user password", DefaultKeyPassword },
			{ "userpassword", DefaultKeyPassword },
			{ DefaultKeyDialect, DefaultKeyDialect },
			{ DefaultKeyPooling, DefaultKeyPooling },
			{ DefaultKeyMaxPoolSize, DefaultKeyMaxPoolSize },
			{ "maxpoolsize", DefaultKeyMaxPoolSize },
			{ DefaultKeyMinPoolSize, DefaultKeyMinPoolSize },
			{ "minpoolsize", DefaultKeyMinPoolSize },
			{ DefaultKeyCharacterSet, DefaultKeyCharacterSet },
			{ "charset", DefaultKeyCharacterSet },
			{ DefaultKeyConnectionLifetime, DefaultKeyConnectionLifetime },
			{ "connectionlifetime", DefaultKeyConnectionLifetime },
			{ "timeout", DefaultKeyConnectionTimeout },
			{ DefaultKeyConnectionTimeout, DefaultKeyConnectionTimeout },
			{ "connectiontimeout", DefaultKeyConnectionTimeout },
			{ DefaultKeyPacketSize, DefaultKeyPacketSize },
			{ "packetsize", DefaultKeyPacketSize },
			{ "role", DefaultKeyRoleName },
			{ DefaultKeyRoleName, DefaultKeyRoleName },
			{ DefaultKeyFetchSize, DefaultKeyFetchSize },
			{ "fetchsize", DefaultKeyFetchSize },
			{ DefaultKeyServerType, DefaultKeyServerType },
			{ "servertype", DefaultKeyServerType },
			{ DefaultKeyIsolationLevel, DefaultKeyIsolationLevel },
			{ "isolationlevel", DefaultKeyIsolationLevel },
			{ DefaultKeyRecordsAffected, DefaultKeyRecordsAffected },
			{ DefaultKeyEnlist, DefaultKeyEnlist },
			{ "clientlibrary", DefaultKeyClientLibrary },
			{ DefaultKeyClientLibrary, DefaultKeyClientLibrary },
			{ DefaultKeyDbCachePages, DefaultKeyDbCachePages },
			{ "cachepages", DefaultKeyDbCachePages },
			{ "pagebuffers", DefaultKeyDbCachePages },
			{ "page buffers", DefaultKeyDbCachePages },
			{ DefaultKeyNoDbTriggers, DefaultKeyNoDbTriggers },
			{ "nodbtriggers", DefaultKeyNoDbTriggers },
			{ "no dbtriggers", DefaultKeyNoDbTriggers },
			{ "no database triggers", DefaultKeyNoDbTriggers },
			{ "nodatabasetriggers", DefaultKeyNoDbTriggers },
			{ DefaultKeyNoGarbageCollect, DefaultKeyNoGarbageCollect },
			{ "nogarbagecollect", DefaultKeyNoGarbageCollect },
			{ DefaultKeyCompression, DefaultKeyCompression },
			{ "wire compression", DefaultKeyCompression },
			{ DefaultKeyCryptKey, DefaultKeyCryptKey },
			{ "cryptkey", DefaultKeyCryptKey },
		};

		internal static readonly IDictionary<string, object> DefaultValues = new Dictionary<string, object>(StringComparer.Ordinal)
		{
			{ DefaultKeyDataSource, DefaultValueDataSource },
			{ DefaultKeyPortNumber, DefaultValuePortNumber },
			{ DefaultKeyUserId, DefaultValueUserId },
			{ DefaultKeyPassword, DefaultValuePassword },
			{ DefaultKeyRoleName, DefaultValueRoleName },
			{ DefaultKeyCatalog, DefaultValueCatalog },
			{ DefaultKeyCharacterSet, DefaultValueCharacterSet },
			{ DefaultKeyDialect, DefaultValueDialect },
			{ DefaultKeyPacketSize, DefaultValuePacketSize },
			{ DefaultKeyPooling, DefaultValuePooling },
			{ DefaultKeyConnectionLifetime, DefaultValueConnectionLifetime },
			{ DefaultKeyMinPoolSize, DefaultValueMinPoolSize },
			{ DefaultKeyMaxPoolSize, DefaultValueMaxPoolSize },
			{ DefaultKeyConnectionTimeout, DefaultValueConnectionTimeout },
			{ DefaultKeyFetchSize, DefaultValueFetchSize },
			{ DefaultKeyServerType, DefaultValueServerType },
			{ DefaultKeyIsolationLevel, DefaultValueIsolationLevel },
			{ DefaultKeyRecordsAffected, DefaultValueRecordsAffected },
			{ DefaultKeyEnlist, DefaultValueEnlist },
			{ DefaultKeyClientLibrary, DefaultValueClientLibrary },
			{ DefaultKeyDbCachePages, DefaultValueDbCachePages },
			{ DefaultKeyNoDbTriggers, DefaultValueNoDbTriggers },
			{ DefaultKeyNoGarbageCollect, DefaultValueNoGarbageCollect },
			{ DefaultKeyCompression, DefaultValueCompression },
			{ DefaultKeyCryptKey, DefaultValueCryptKey },
		};

		#endregion

		#region Fields

		private Dictionary<string, object> _options;
		private bool _isServiceConnectionString;

		#endregion

		#region Properties

		public string UserID => GetString(DefaultKeyUserId);
		public string Password => GetString(DefaultKeyPassword);
		public string DataSource => GetString(DefaultKeyDataSource);
		public int Port => GetInt32(DefaultKeyPortNumber);
		public string Database => ExpandDataDirectory(GetString(DefaultKeyCatalog));
		public short PacketSize => GetInt16(DefaultKeyPacketSize);
		public string Role => GetString(DefaultKeyRoleName);
		public byte Dialect => GetByte(DefaultKeyDialect);
		public string Charset => GetString(DefaultKeyCharacterSet);
		public int ConnectionTimeout => GetInt32(DefaultKeyConnectionTimeout);
		public bool Pooling => GetBoolean(DefaultKeyPooling);
		public long ConnectionLifeTime => GetInt64(DefaultKeyConnectionLifetime);
		public int MinPoolSize => GetInt32(DefaultKeyMinPoolSize);
		public int MaxPoolSize => GetInt32(DefaultKeyMaxPoolSize);
		public int FetchSize => GetInt32(DefaultKeyFetchSize);
		public FbServerType ServerType => (FbServerType)GetInt32(DefaultKeyServerType);
		public IsolationLevel IsolationLevel => GetIsolationLevel(DefaultKeyIsolationLevel);
		public bool ReturnRecordsAffected => GetBoolean(DefaultKeyRecordsAffected);
		public bool Enlist => GetBoolean(DefaultKeyEnlist);
		public string ClientLibrary => GetString(DefaultKeyClientLibrary);
		public int DbCachePages => GetInt32(DefaultKeyDbCachePages);
		public bool NoDatabaseTriggers => GetBoolean(DefaultKeyNoDbTriggers);
		public bool NoGarbageCollect => GetBoolean(DefaultKeyNoGarbageCollect);
		public bool Compression => GetBoolean(DefaultKeyCompression);
		public byte[] CryptKey => GetBytes(DefaultKeyCryptKey);

		#endregion

		#region Internal Properties
		internal string NormalizedConnectionString
		{
			get { return string.Join(";", _options.OrderBy(x => x.Key, StringComparer.Ordinal).Where(x => x.Value != null).Select(x => string.Format("{0}={1}", x.Key, WrapValueIfNeeded(x.Value.ToString())))); }
		}
		#endregion

		#region Constructors

		public FbConnectionString()
		{
			SetDefaultOptions();
		}

		public FbConnectionString(string connectionString)
		{
			Load(connectionString);
		}

		internal FbConnectionString(bool isServiceConnectionString)
		{
			_isServiceConnectionString = isServiceConnectionString;
			SetDefaultOptions();
		}

		#endregion

		#region Methods

		public void Load(string connectionString)
		{
			const string KeyPairsRegex = "(([\\w\\s\\d]*)\\s*?=\\s*?\"([^\"]*)\"|([\\w\\s\\d]*)\\s*?=\\s*?'([^']*)'|([\\w\\s\\d]*)\\s*?=\\s*?([^\"';][^;]*))";

			SetDefaultOptions();

			if (connectionString != null && connectionString.Length > 0)
			{
				MatchCollection keyPairs = Regex.Matches(connectionString, KeyPairsRegex);

				foreach (Match keyPair in keyPairs)
				{
					if (keyPair.Groups.Count == 8)
					{
						string[] values = new string[]
						{
							(keyPair.Groups[2].Success ? keyPair.Groups[2].Value
								: keyPair.Groups[4].Success ? keyPair.Groups[4].Value
									: keyPair.Groups[6].Success ? keyPair.Groups[6].Value
										: string.Empty)
							.Trim().ToLowerInvariant(),
							(keyPair.Groups[3].Success ? keyPair.Groups[3].Value
								: keyPair.Groups[5].Success ? keyPair.Groups[5].Value
									: keyPair.Groups[7].Success ? keyPair.Groups[7].Value
										: string.Empty)
							.Trim()
						};

						if (values.Length == 2 && !string.IsNullOrEmpty(values[0]) && !string.IsNullOrEmpty(values[1]))
						{
							if (Synonyms.TryGetValue(values[0], out var key))
							{
								switch (key)
								{
									case DefaultKeyServerType:
										var serverType = default(FbServerType);
										try
										{
											serverType = (FbServerType)Enum.Parse(typeof(FbServerType), values[1], true);
										}
										catch
										{
											throw new NotSupportedException($"Not supported '{DefaultKeyServerType}'.");
										}
										_options[key] = serverType;
										break;
									case DefaultKeyCryptKey:
										var cryptKey = default(byte[]);
										try
										{
											cryptKey = Convert.FromBase64String(values[1]);
										}
										catch
										{
											throw new NotSupportedException($"Not supported '{DefaultKeyCryptKey}'.");
										}
										_options[key] = cryptKey;
										break;
									default:
										_options[key] = values[1];
										break;
								}
							}
						}
					}
				}

				if (Database != null && Database.Length > 0)
				{
					ParseConnectionInfo(Database);
				}
			}
		}

		public void Validate()
		{
			if (
				(string.IsNullOrEmpty(Database) && !_isServiceConnectionString) ||
				(string.IsNullOrEmpty(DataSource) && ServerType != FbServerType.Embedded) ||
				(string.IsNullOrEmpty(Charset)) ||
				(Port == 0) ||
				(!Enum.IsDefined(typeof(FbServerType), ServerType)) ||
				(MinPoolSize > MaxPoolSize)
			   )
			{
				throw new ArgumentException("An invalid connection string argument has been supplied or a required connection string argument has not been supplied.");
			}
			if (Dialect < 1 || Dialect > 3)
			{
				throw new ArgumentException("Incorrect database dialect it should be 1, 2, or 3.");
			}
			if (PacketSize < 512 || PacketSize > 32767)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "'Packet Size' value of {0} is not valid.{1}The value should be an integer >= 512 and <= 32767.", PacketSize, Environment.NewLine));
			}
			if (DbCachePages < 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "'Cache Pages' value of {0} is not valid.{1}The value should be an integer >= 0.", DbCachePages, Environment.NewLine));
			}
			if (Pooling && NoDatabaseTriggers)
			{
				throw new ArgumentException("Cannot use Pooling and NoDBTriggers together.");
			}

			CheckIsolationLevel();
		}

		#endregion

		#region Private Methods

		private void SetDefaultOptions()
		{
			_options = new Dictionary<string, object>(DefaultValues);
		}

		private void ParseConnectionInfo(string connectInfo)
		{
			var database = (string)null;
			var dataSource = (string)null;
			var portNumber = -1;

			// allows standard syntax //host:port/....
			// and old fb syntax host/port:....
			connectInfo = connectInfo.Trim();
			char hostSepChar;
			char portSepChar;

			if (connectInfo.StartsWith("//"))
			{
				connectInfo = connectInfo.Substring(2);
				hostSepChar = '/';
				portSepChar = ':';
			}
			else
			{
				hostSepChar = ':';
				portSepChar = '/';
			}

			int sep = connectInfo.IndexOf(hostSepChar);
			if (sep == 0 || sep == connectInfo.Length - 1)
			{
				throw new ArgumentException("An invalid connection string argument has been supplied or a required connection string argument has not been supplied.");
			}
			else if (sep > 0)
			{
				dataSource = connectInfo.Substring(0, sep);
				database = connectInfo.Substring(sep + 1);
				int portSep = dataSource.IndexOf(portSepChar);

				if (portSep == 0 || portSep == dataSource.Length - 1)
				{
					throw new ArgumentException("An invalid connection string argument has been supplied or a required connection string argument has not been supplied.");
				}
				else if (portSep > 0)
				{
					portNumber = Int32.Parse(dataSource.Substring(portSep + 1), CultureInfo.InvariantCulture);
					dataSource = dataSource.Substring(0, portSep);
				}
				else if (portSep < 0 && dataSource.Length == 1)
				{
					if (string.IsNullOrEmpty(DataSource))
					{
						dataSource = "localhost";
					}
					else
					{
						dataSource = null;
					}

					database = connectInfo;
				}
			}
			else if (sep == -1)
			{
				database = connectInfo;
			}

			_options[DefaultKeyCatalog] = database;
			if (dataSource != null)
			{
				_options[DefaultKeyDataSource] = dataSource;
			}
			if (portNumber != -1)
			{
				_options[DefaultKeyPortNumber] = portNumber;
			}
		}

		private string ExpandDataDirectory(string s)
		{
			const string DataDirectoryKeyword = "|DataDirectory|";
#if NETSTANDARD1_6
			if (s.IndexOf(DataDirectoryKeyword, StringComparison.OrdinalIgnoreCase) != -1)
				throw new NotSupportedException();

			return s;
#else
			if (s == null)
				return s;

			var dataDirectoryLocation = (string)AppDomain.CurrentDomain.GetData("DataDirectory") ?? string.Empty;
			var pattern = string.Format("{0}{1}?", Regex.Escape(DataDirectoryKeyword), Regex.Escape(Path.DirectorySeparatorChar.ToString()));
			return Regex.Replace(s, pattern, dataDirectoryLocation + Path.DirectorySeparatorChar, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
#endif
		}

		private string GetString(string key)
		{
			return _options.TryGetValue(key, out var value)
				? (string)value
				: null;
		}

		private bool GetBoolean(string key)
		{
			return _options.TryGetValue(key, out var value)
				? bool.Parse(value.ToString())
				: false;
		}

		private byte GetByte(string key)
		{
			return _options.TryGetValue(key, out var value)
				? Convert.ToByte(value, CultureInfo.CurrentCulture)
				: (byte)0;
		}

		private short GetInt16(string key)
		{
			return _options.TryGetValue(key, out var value)
				? Convert.ToInt16(value, CultureInfo.InvariantCulture)
				: (short)0;
		}

		private int GetInt32(string key)
		{
			return _options.TryGetValue(key, out var value)
				? Convert.ToInt32(value, CultureInfo.InvariantCulture)
				: 0;
		}

		private long GetInt64(string key)
		{
			return _options.TryGetValue(key, out var value)
				? Convert.ToInt64(value, CultureInfo.InvariantCulture)
				: 0;
		}

		private byte[] GetBytes(string key)
		{
			return _options.TryGetValue(key, out var value)
				? (byte[])value
				: null;
		}

		private IsolationLevel GetIsolationLevel(string key)
		{
			if (_options.TryGetValue(key, out var value))
			{
				var il = value.ToString().ToLowerInvariant();

				switch (il)
				{
					case "readcommitted":
						return IsolationLevel.ReadCommitted;

					case "readuncommitted":
						return IsolationLevel.ReadUncommitted;

					case "repeatableread":
						return IsolationLevel.RepeatableRead;

					case "serializable":
						return IsolationLevel.Serializable;

					case "chaos":
						return IsolationLevel.Chaos;

					case "snapshot":
						return IsolationLevel.Snapshot;

					case "unspecified":
						return IsolationLevel.Unspecified;
				}
			}

			return IsolationLevel.ReadCommitted;
		}

		private void CheckIsolationLevel()
		{
			var il = _options[DefaultKeyIsolationLevel].ToString().ToLowerInvariant();

			switch (il)
			{
				case "readcommitted":
				case "readuncommitted":
				case "repeatableread":
				case "serializable":
				case "chaos":
				case "unspecified":
				case "snapshot":
					break;

				default:
					throw new ArgumentException("Specified Isolation Level is not valid.");
			}
		}

		private string WrapValueIfNeeded(string value)
		{
			if (value != null && value.Contains(";"))
				return "'" + value + "'";
			return value;
		}

		#endregion
	}
}
