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

//$Authors = Carlos Guzman Alvarez, Jiri Cincura (jiri@cincura.net), Vladimir Bodecek

using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Net;
using System.Collections.Generic;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed.Version11
{
	internal class GdsDatabase : Version10.GdsDatabase
	{
		#region Constructors

		public GdsDatabase(GdsConnection connection)
			: base(connection)
		{
			DeferredPackets = new Queue<Action<IResponse>>();
		}

		#endregion

		#region Properties
		public Queue<Action<IResponse>> DeferredPackets { get; private set; }
		#endregion

		#region Override Statement Creation Methods

		public override StatementBase CreateStatement()
		{
			return new GdsStatement(this);
		}

		public override StatementBase CreateStatement(TransactionBase transaction)
		{
			return new GdsStatement(this, transaction);
		}

		#endregion

		#region Trusted Auth
		public override void AttachWithTrustedAuth(DatabaseParameterBuffer dpb, string dataSource, int port, string database, byte[] cryptKey)
		{
			try
			{
				using (SspiHelper sspiHelper = new SspiHelper())
				{
					byte[] authData = sspiHelper.InitializeClientSecurity();
					SendTrustedAuthToBuffer(dpb, authData);
					SendAttachToBuffer(dpb, database);
					XdrStream.Flush();

					IResponse response = ReadResponse();
					ProcessTrustedAuthResponse(sspiHelper, ref response);
					ProcessAttachResponse((GenericResponse)response);
				}
			}
			catch (IscException)
			{
				SafelyDetach();
				throw;
			}
			catch (IOException ex)
			{
				SafelyDetach();
				throw IscException.ForErrorCode(IscCodes.isc_net_write_err, ex);
			}

			AfterAttachActions();
		}

		protected virtual void SendTrustedAuthToBuffer(DatabaseParameterBuffer dpb, byte[] authData)
		{
			dpb.Append(IscCodes.isc_dpb_trusted_auth, authData);
		}

		protected void ProcessTrustedAuthResponse(SspiHelper sspiHelper, ref IResponse response)
		{
			while (response is AuthResponse)
			{
				byte[] authData = sspiHelper.GetClientSecurity(((AuthResponse)response).Data);
				XdrStream.Write(IscCodes.op_trusted_auth);
				XdrStream.WriteBuffer(authData);
				XdrStream.Flush();
				response = ReadResponse();
			}
		}
		#endregion

		#region Public methods
		public override void ReleaseObject(int op, int id)
		{
			try
			{
				DoReleaseObjectPacket(op, id);
				DeferredPackets.Enqueue(ProcessReleaseObjectResponse);
			}
			catch (IOException ex)
			{
				throw IscException.ForErrorCode(IscCodes.isc_net_read_err, ex);
			}
		}

		public override int ReadOperation()
		{
			ProcessDeferredPackets();
			return base.ReadOperation();
		}

		public override int NextOperation()
		{
			ProcessDeferredPackets();
			return base.NextOperation();
		}
		#endregion

		#region Private methods
		private void ProcessDeferredPackets()
		{
			if (DeferredPackets.Count > 0)
			{
				// copy it to local collection and clear to not get same processing when the method is hit again from ReadSingleResponse
				Action<IResponse>[] methods = DeferredPackets.ToArray();
				DeferredPackets.Clear();
				foreach (Action<IResponse> method in methods)
				{
					method(ReadSingleResponse());
				}
			}
		}
		#endregion
	}
}
