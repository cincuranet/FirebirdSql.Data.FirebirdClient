using System;
using System.Transactions;

namespace FirebirdSql.Data.FirebirdClient
{
	public class FbDtcTransactionHandler : MarshalByRefObject
	{
		private readonly FbConnectionInternal _connection;
		private FbTransaction _tx;
		private readonly string _id = Guid.NewGuid().ToString();

		internal FbDtcTransactionHandler(FbConnectionInternal connection, IsolationLevel isolationLevel)
		{
			_connection = connection;
			if (connection.HasActiveTransaction)
				_tx = connection.ActiveTransaction;
			else
				_tx = connection.BeginTransaction(isolationLevel);
		}

		public void CommitTransaction()
		{
			if (_tx == null)
				return;

			_tx.Commit();
			TransactionCompleted();
		}

		public void PrepareTransaction()
		{
			if (_tx == null)
				return;

			_tx.Transaction.Prepare();
		}

		public void RollbackTransaction()
		{
			if (_tx == null)
				return;

			_tx.Rollback();
			TransactionCompleted();
		}

		public string Id { get { return _id; } }

		private void TransactionCompleted()
		{
			_tx.Dispose();
			_tx = null;
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
