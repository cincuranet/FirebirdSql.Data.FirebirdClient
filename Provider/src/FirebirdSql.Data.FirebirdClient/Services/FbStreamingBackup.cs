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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FirebirdSql.Data.Common;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdSql.Data.Services
{
	public sealed class FbStreamingBackup : FbService
	{
		public string SkipData { get; set; }
		public FbBackupFlags Options { get; set; }
		public Stream OutputStream { get; set; }

		public FbStreamingBackup(string connectionString = null)
			: base(connectionString)
		{ }

		public void Execute()
		{
			try
			{
				StartSpb = new ServiceParameterBuffer();
				StartSpb.Append(IscCodes.isc_action_svc_backup);
				StartSpb.Append(IscCodes.isc_spb_dbname, Database);
				StartSpb.Append(IscCodes.isc_spb_bkp_file, "stdout");
				if (!string.IsNullOrEmpty(SkipData))
					StartSpb.Append(IscCodes.isc_spb_bkp_skip_data, SkipData);
				StartSpb.Append(IscCodes.isc_spb_options, (int)Options);

				Open();
				StartTask();
				ReadOutput();
			}
			catch (Exception ex)
			{
				throw new FbException(ex.Message, ex);
			}
			finally
			{
				Close();
			}
		}

		void ReadOutput()
		{
			Query(new byte[] { IscCodes.isc_info_svc_to_eof }, (_, x) =>
			{
				var buffer = x as byte[];
				OutputStream.Write(buffer, 0, buffer.Length);
			});
		}
	}
}
