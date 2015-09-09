﻿/*
 *	Firebird ADO.NET Data provider for .NET and Mono
 *
 *	   The contents of this file are subject to the Initial
 *	   Developer's Public License Version 1.0 (the "License");
 *	   you may not use this file except in compliance with the
 *	   License. You may obtain a copy of the License at
 *	   http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *	   Software distributed under the License is distributed on
 *	   an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *	   express or implied. See the License for the specific
 *	   language governing rights and limitations under the License.
 *
 *	Copyright (c) 2009-2010 Jiri Cincura (jiri@cincura.net)
 *
 *	All Rights Reserved.
 */

using System;
using System.Collections;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Net;
using System.Collections.Generic;

using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Client.Managed.Version13
{
	internal class GdsDatabase : Version12.GdsDatabase
	{
		public GdsDatabase(Version10.GdsConnection connection)
			: base(connection)
		{ }

		protected override void SendAttachToBuffer(DatabaseParameterBuffer dpb, string database)
		{
			// Attach to the database
			Write(IscCodes.op_attach);
			Write(0);					// Database	object ID
			if (!string.IsNullOrEmpty(UserID)) {
				dpb.Append(IscCodes.isc_dpb_user_name, UserID);
				if (AuthData != null) {
					dpb.Append(IscCodes.isc_dpb_specific_auth_data, Encoding.UTF8.GetBytes(BitConverter.ToString(AuthData).Replace("-", string.Empty)));
				}
			}
			dpb.Append(IscCodes.isc_dpb_utf8_filename, 0);
			WriteBuffer(Encoding.UTF8.GetBytes(database));				// Database	PATH
			WriteBuffer(dpb.ToArray());	// DPB Parameter buffer
		}

		protected override void SendCreateToBuffer(DatabaseParameterBuffer dpb, string database)
		{
			Write(IscCodes.op_create);
			Write(0);
			if (!string.IsNullOrEmpty(UserID)) {
				dpb.Append(IscCodes.isc_dpb_user_name, UserID);
				if (AuthData != null) {
					dpb.Append(IscCodes.isc_dpb_specific_auth_data, Encoding.UTF8.GetBytes(BitConverter.ToString(AuthData).Replace("-", string.Empty)));
				}
			}
			dpb.Append(IscCodes.isc_dpb_utf8_filename, 0);
			WriteBuffer(Encoding.UTF8.GetBytes(database));

			WriteBuffer(dpb.ToArray());
		}

		#region Override Statement Creation Methods

		public override StatementBase CreateStatement()
		{
			return new GdsStatement(this);
		}

		public override StatementBase CreateStatement(ITransaction transaction)
		{
			return new GdsStatement(this, transaction);
		}

		#endregion

		#region Cancel Methods

		public override void CancelOperation(int kind)
		{
			try
			{
				SendCancelOperationToBuffer(kind);
				Flush();
				// no response, this is async
			}
			catch (IOException)
			{
				throw new IscException(IscCodes.isc_network_error);
			}
		}

		protected void SendCancelOperationToBuffer(int kind)
		{
			Write(IscCodes.op_cancel);
			Write(kind);
		}

		#endregion
	}
}
