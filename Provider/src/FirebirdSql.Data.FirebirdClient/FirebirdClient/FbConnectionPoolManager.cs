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

//$Authors = Jiri Cincura (jiri@cincura.net)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.FirebirdClient
{
	sealed class FbConnectionPoolManager : IDisposable
	{
		static Lazy<FbConnectionPoolManager> _instanceLazy = new Lazy<FbConnectionPoolManager>(() => new FbConnectionPoolManager(), LazyThreadSafetyMode.ExecutionAndPublication);

		internal static FbConnectionPoolManager Instance
		{
			get { return _instanceLazy.Value; }
		}

		sealed class Pool : IDisposable
		{
			sealed class Item : IDisposable
			{
				bool _disposed;

				public long Created { get; private set; }
				public FbConnectionInternal Connection { get; private set; }

				public Item(long created, FbConnectionInternal connection)
				{
					Created = created;
					Connection = connection;
				}

				public void Dispose()
				{
					if (_disposed)
						return;
					_disposed = true;
					Created = default(long);
					Connection.Dispose();
					Connection = null;
				}
			}

			bool _disposed;
			object _syncRoot;
			FbConnectionString _connectionString;
			Stack<Item> _available;
			List<FbConnectionInternal> _busy;

			public Pool(FbConnectionString connectionString)
			{
				_syncRoot = new object();
				_connectionString = connectionString;
				_available = new Stack<Item>();
				_busy = new List<FbConnectionInternal>();
			}

			public void Dispose()
			{
				lock (_syncRoot)
				{
					if (_disposed)
						return;
					_disposed = true;
					_connectionString = null;
					CleanConnectionsImpl();
					_available = null;
					_busy = null;
				}
			}

			public FbConnectionInternal GetConnection(FbConnection owner)
			{
				lock (_syncRoot)
				{
					CheckDisposedImpl();

					var connection = _available.Any()
						? _available.Pop().Connection
						: CreateNewConnectionIfPossibleImpl(_connectionString);
					connection.SetOwningConnection(owner);
					_busy.Add(connection);
					return connection;
				}
			}

			public void ReleaseConnection(FbConnectionInternal connection)
			{
				lock (_syncRoot)
				{
					CheckDisposedImpl();

					var removed = _busy.Remove(connection);
					if (removed)
					{
						_available.Push(new Item(GetTicks(), connection));
					}
				}
			}

			public void CleanupPool()
			{
				lock (_syncRoot)
				{
					CheckDisposedImpl();

					var now = GetTicks();
					var available = _available.ToArray();
					if (available.Count() <= _connectionString.MinPoolSize)
						return;
					var keep = available.Where(x => IsAlive(_connectionString.ConnectionLifeTime, x.Created, now)).ToArray();
					var keepCount = keep.Count();
					if (keepCount < _connectionString.MinPoolSize)
					{
						keep = keep.Concat(available.Except(keep).OrderByDescending(x => x.Created).Take(_connectionString.MinPoolSize - keepCount)).ToArray();
					}
					var release = available.Except(keep).ToArray();
					release.AsParallel().ForAll(x => x.Dispose());
					_available = new Stack<Item>(keep);
				}
			}

			public void ClearPool()
			{
				lock (_syncRoot)
				{
					CheckDisposedImpl();

					CleanConnectionsImpl();
					_available.Clear();
					_busy.Clear();
				}
			}

			static FbConnectionInternal CreateNewConnection(FbConnectionString connectionString)
			{
				var result = new FbConnectionInternal(connectionString);
				result.Connect();
				return result;
			}

			static bool IsAlive(long connectionLifeTime, long created, long now)
			{
				if (connectionLifeTime == 0)
					return true;
				return (now - created) > (connectionLifeTime * 1000);
			}

			static long GetTicks()
			{
				var ticks = Environment.TickCount;
				return ticks + -(long)int.MinValue;
			}

			void CleanConnectionsImpl()
			{
				Parallel.Invoke(
					() =>
					{
						foreach (var item in _available)
							item.Dispose();
					},
					() =>
					{
						foreach (var item in _busy)
							item.Dispose();
					});
			}

			void CheckDisposedImpl()
			{
				if (_disposed)
					throw new ObjectDisposedException(nameof(Pool));
			}

			FbConnectionInternal CreateNewConnectionIfPossibleImpl(FbConnectionString connectionString)
			{
				if (_busy.Count() + 1 > connectionString.MaxPoolSize)
					throw new InvalidOperationException("Connection pool is full.");
				return CreateNewConnection(connectionString);
			}
		}

		int _disposed;
		ConcurrentDictionary<string, Pool> _pools;
		Timer _cleanupTimer;

		FbConnectionPoolManager()
		{
			_disposed = 0;
			_pools = new ConcurrentDictionary<string, Pool>();
			_cleanupTimer = new Timer(CleanupCallback, null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
		}

		internal FbConnectionInternal Get(FbConnectionString connectionString, FbConnection owner)
		{
			CheckDisposed();

			return _pools.GetOrAdd(connectionString.NormalizedConnectionString, _ => new Pool(connectionString)).GetConnection(owner);
		}

		internal void Release(FbConnectionInternal connection)
		{
			CheckDisposed();

			_pools.GetOrAdd(connection.Options.NormalizedConnectionString, _ => new Pool(connection.Options)).ReleaseConnection(connection);
		}

		internal void ClearAllPools()
		{
			CheckDisposed();

			_pools.Values.AsParallel().ForAll(p => p.ClearPool());
		}

		internal void ClearPool(FbConnectionString connectionString)
		{
			CheckDisposed();

			if (_pools.TryGetValue(connectionString.NormalizedConnectionString, out var pool))
			{
				pool.ClearPool();
			}
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) == 1)
				return;
			_cleanupTimer.Dispose();
			_cleanupTimer = null;
			_pools.Values.AsParallel().ForAll(x => x.Dispose());
			_pools = null;
		}

		void CleanupCallback(object o)
		{
			if (Volatile.Read(ref _disposed) == 1)
				return;
			_pools.Values.AsParallel().ForAll(x => x.CleanupPool());
			_cleanupTimer.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
		}

		void CheckDisposed()
		{
			if (Volatile.Read(ref _disposed) == 1)
				throw new ObjectDisposedException(nameof(FbConnectionPoolManager));
		}
	}
}
