using System;
using System.Runtime.Remoting.Lifetime;
using System.Transactions;

namespace FirebirdSql.Data.FirebirdClient
{
	public class FbInternalResourceManager : IPromotableSinglePhaseNotification
	{
		private readonly FbConnectionInternal _connection;
		private FbTransaction _fbTransaction;
		private IsolationLevel _isolationLevel;
		private FbDtcTransactionHandler _txHandler;

		internal FbInternalResourceManager(FbConnectionInternal connection)
		{
			_connection = connection;
		}

		public void Enlist(Transaction tx)
		{
			if (tx != null)
			{
				_isolationLevel = tx.IsolationLevel;
				if (!tx.EnlistPromotableSinglePhase(this))
				{
					InitResourceManager();
					_txHandler = new FbDtcTransactionHandler(_connection, _isolationLevel);
					_resourceManager.Enlist(_txHandler, TransactionInterop.GetTransmitterPropagationToken(tx));
				}
			}
		}

		public bool IsCompleted { get; private set; }
		public Transaction SystemTransaction { get; private set; }

		#region IPromotableSinglePhaseNotification Members

		public void Initialize()
		{
			_fbTransaction = _connection.BeginTransaction(_isolationLevel);
		}

		public void Rollback(SinglePhaseEnlistment singlePhaseEnlistment)
		{
			if (_fbTransaction != null)
			{
				_fbTransaction.Rollback();
				_fbTransaction.Dispose();
				_fbTransaction = null;
				singlePhaseEnlistment.Aborted();
				PromotableLocalTransactionCompleted();
			}
			else
			{
				if (_txHandler != null)
				{
					if (_resourceManager != null)
					{
						_resourceManager.RollbackWork(_txHandler.Id);
						singlePhaseEnlistment.Aborted();
					}
					else
					{
						_txHandler.RollbackTransaction();
						singlePhaseEnlistment.Aborted();
					}
					_txHandler = null;
				}
			}
		}

		public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
		{
			if (_fbTransaction != null)
			{
				_fbTransaction.Commit();
				_fbTransaction.Dispose();
				_fbTransaction = null;
				singlePhaseEnlistment.Committed();
				PromotableLocalTransactionCompleted();
			}
			else
			{
				if (_txHandler != null)
				{
					if (_resourceManager != null)
					{
						_resourceManager.CommitWork(_txHandler.Id);
						singlePhaseEnlistment.Committed();
					}
					else
					{
						_txHandler.CommitTransaction();
						singlePhaseEnlistment.Committed();
					}
					_txHandler = null;
				}
			}
		}

		#endregion

		#region ITransactionPromoter Members

		public byte[] Promote()
		{
			if (_fbTransaction != null)
			{
				_fbTransaction = null;
			}

			InitResourceManager();
			if (_txHandler == null)
			{
				_txHandler = new FbDtcTransactionHandler(_connection, _isolationLevel);
			}
			byte[] token = _resourceManager.Promote(_txHandler);

			return token;
		}

		#endregion

		private static FbResourceManager _resourceManager;
		private static ClientSponsor _clntSponser;
		private static void InitResourceManager()
		{
			if (_resourceManager == null)
			{
				_clntSponser = new ClientSponsor();
				AppDomain rmDomain = AppDomain.CreateDomain("FbTransactionConductor", AppDomain.CurrentDomain.Evidence, AppDomain.CurrentDomain.SetupInformation);

				var assemblyFullName = typeof(FbResourceManager).Assembly.FullName;
				var fullName = typeof(FbResourceManager).FullName;
				_resourceManager = (FbResourceManager)rmDomain.CreateInstanceAndUnwrap(assemblyFullName, fullName);
				_clntSponser.Register(_resourceManager);
			}
		}

		private void PromotableLocalTransactionCompleted()
		{
			_connection.PromotableLocalTransactionCompleted();
			if (_connection != null)
			{
				if (!_connection.Options.Pooling && (_connection.OwningConnection == null || _connection.OwningConnection.IsClosed))
				{
					_connection.Disconnect();
				}
			}
		}
	}
}
