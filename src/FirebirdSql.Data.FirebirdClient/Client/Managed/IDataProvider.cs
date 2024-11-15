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

//$Authors = Jiri Cincura (jiri@cincura.net)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace FirebirdSql.Data.Client.Managed;

interface IDataProvider
{
	int Read(byte[] buffer, int offset, int count);
	ValueTask<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

	void Write(ReadOnlySpan<byte> buffer);
	void Write(byte[] buffer, int offset, int count);

	ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);
	ValueTask WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

	void Flush();
	ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
