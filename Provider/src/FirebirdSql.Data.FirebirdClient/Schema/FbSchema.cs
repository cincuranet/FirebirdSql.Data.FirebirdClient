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

//$Authors = Carlos Guzman Alvarez, Jiri Cincura (jiri@cincura.net)

#if !NETSTANDARD1_6
using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Schema
{
	internal abstract class FbSchema
	{
		#region Constructors

		public FbSchema()
		{
		}

		#endregion

		#region Abstract Methods

		protected abstract StringBuilder GetCommandText(string[] restrictions);

		#endregion

		#region Methods

		public DataTable GetSchema(FbConnection connection, string collectionName, string[] restrictions)
		{
			DataTable dataTable = new DataTable(collectionName);
			using (FbCommand command = BuildCommand(connection, collectionName, ParseRestrictions(restrictions)))
			{
				using (FbDataAdapter adapter = new FbDataAdapter(command))
				{
					try
					{
						adapter.Fill(dataTable);
					}
					catch (Exception ex)
					{
						throw new FbException(ex.Message);
					}
				}
			}
			TrimStringFields(dataTable);
			return ProcessResult(dataTable);
		}

		#endregion

		#region Protected Methods

		protected FbCommand BuildCommand(FbConnection connection, string collectionName, string[] restrictions)
		{
			string filter = String.Format("CollectionName='{0}'", collectionName);
			StringBuilder builder = GetCommandText(restrictions);
			DataRow[] restriction = connection.GetSchema(DbMetaDataCollectionNames.Restrictions).Select(filter);
			FbTransaction transaction = connection.InnerConnection.ActiveTransaction;
			FbCommand command = new FbCommand(builder.ToString(), connection, transaction);

			if (restrictions != null && restrictions.Length > 0)
			{
				int index = 0;

				for (int i = 0; i < restrictions.Length; i++)
				{
					string rname = restriction[i]["RestrictionName"].ToString();
					if (restrictions[i] != null)
					{
						// Catalog, Schema and TableType are no real restrictions
						if (!rname.EndsWith("Catalog") && !rname.EndsWith("Schema") && rname != "TableType")
						{
							string pname = String.Format(CultureInfo.CurrentUICulture, "@p{0}", index++);

							command.Parameters.Add(pname, FbDbType.VarChar, 255).Value = restrictions[i];
						}
					}
				}
			}

			return command;
		}

		protected virtual DataTable ProcessResult(DataTable schema)
		{
			return schema;
		}

		protected virtual string[] ParseRestrictions(string[] restrictions)
		{
			return restrictions;
		}

		#endregion

		#region Private Static Methods

		private static void TrimStringFields(DataTable schema)
		{
			schema.BeginLoadData();

			foreach (DataRow row in schema.Rows)
			{
				for (int i = 0; i < schema.Columns.Count; i++)
				{
					if (!row.IsNull(schema.Columns[i]) &&
						schema.Columns[i].DataType == typeof(System.String))
					{
						row[schema.Columns[i]] = row[schema.Columns[i]].ToString().Trim();
					}
				}
			}

			schema.EndLoadData();
			schema.AcceptChanges();
		}

		#endregion
	}
}
#endif
