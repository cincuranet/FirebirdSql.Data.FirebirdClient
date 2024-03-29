﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/raw/master/license.txt.
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FirebirdSql.Data.TestsBase;
using NUnit.Framework;

namespace FirebirdSql.Data.FirebirdClient.Tests;

[TestFixtureSource(typeof(FbServerTypeTestFixtureSource), nameof(FbServerTypeTestFixtureSource.Default))]
[TestFixtureSource(typeof(FbServerTypeTestFixtureSource), nameof(FbServerTypeTestFixtureSource.Embedded))]
public class FbTransactionInfoTests : FbTestsBase
{
	public FbTransactionInfoTests(FbServerType serverType, bool compression, FbWireCrypt wireCrypt)
		: base(serverType, compression, wireCrypt)
	{ }

	[Test]
	public async Task CompleteTransactionInfoTest()
	{
		await using (var transaction = await Connection.BeginTransactionAsync())
		{
			var txInfo = new FbTransactionInfo(transaction);
			foreach (var m in txInfo.GetType()
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
				.Where(x => !x.IsSpecialName)
				.Where(x => x.Name.EndsWith("Async")))
			{
				if (ServerVersion < new Version(4, 0, 0, 0)
					 && new[] {
						nameof(FbTransactionInfo.GetTransactionSnapshotNumberAsync),
					}.Contains(m.Name))
					continue;

				Assert.DoesNotThrowAsync(() => (Task)m.Invoke(txInfo, new object[] { CancellationToken.None }), m.Name);
			}
		}
	}
}
