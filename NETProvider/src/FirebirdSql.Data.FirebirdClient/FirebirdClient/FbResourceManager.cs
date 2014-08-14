using System;
using System.Collections.Generic;
using System.Transactions;

namespace FirebirdSql.Data.FirebirdClient
{
	public class FbResourceManager : MarshalByRefObject
	{
		private readonly Dictionary<string, CommittableTransaction> _transactions = new Dictionary<string, CommittableTransaction>();

		public void Enlist(FbDtcTransactionHandler txHandler, byte[] txToken)
		{
			FbDurableResourceManager resourceManager = new FbDurableResourceManager(txHandler);
			resourceManager.Enlist(txToken);
		}

		public void CommitWork(string txId)
		{
			CommittableTransaction tx;
			if (_transactions.TryGetValue(txId, out tx))
			{
				tx.Commit();
				_transactions.Remove(txId);
			}
		}

		public void RollbackWork(string txId)
		{
			CommittableTransaction tx;
			if (_transactions.TryGetValue(txId, out tx))
			{
				_transactions.Remove(txId);
				tx.Rollback();
			}
		}

		public byte[] Promote(FbDtcTransactionHandler txHandler)
		{
			CommittableTransaction tx = new CommittableTransaction();
			FbDurableResourceManager resourceManager = new FbDurableResourceManager(txHandler);

			//Promote to MSDTC
			byte[] token = TransactionInterop.GetTransmitterPropagationToken(tx);

			_transactions.Add(resourceManager.TxId, tx);
			resourceManager.Enlist(tx);

			return token;
		}
	}

	class FbDurableResourceManager : ISinglePhaseNotification
	{
		private static readonly Guid _resourceManagerIdentifier = new Guid("426EAAF0-0542-4036-A425-53C132A40872");
		private readonly FbDtcTransactionHandler _txHandler;

		public FbDurableResourceManager(FbDtcTransactionHandler txHandler)
		{
			_txHandler = txHandler;
		}

		public void Enlist(byte[] token)
		{
			Enlist(TransactionInterop.GetTransactionFromTransmitterPropagationToken(token));
		}

		public void Enlist(Transaction tx)
		{
			tx.EnlistDurable(_resourceManagerIdentifier, this, EnlistmentOptions.None);
		}

		public string TxId { get { return _txHandler.Id; } }

		#region ISinglePhaseNotification Members

		public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
		{
			_txHandler.CommitTransaction();
			singlePhaseEnlistment.Committed();
		}

		#endregion

		#region IEnlistmentNotification Members

		public void Commit(Enlistment enlistment)
		{
			_txHandler.CommitTransaction();
			enlistment.Done();
		}

		public void InDoubt(Enlistment enlistment)
		{
			throw new NotImplementedException();
		}

		public void Prepare(PreparingEnlistment preparingEnlistment)
		{
			_txHandler.PrepareTransaction();
			preparingEnlistment.Prepared();
		}

		public void Rollback(Enlistment enlistment)
		{
			_txHandler.RollbackTransaction();
			enlistment.Done();
		}

		#endregion
	}
}
