using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebirdSql.Data.Common
{
	internal static class TaskHelpers
	{
		public static Task<TResult> FromException<TResult>(Exception exc)
		{
			var tcs = new TaskCompletionSource<TResult>();
			tcs.SetException(exc);
			return tcs.Task;
		}
	}
}
