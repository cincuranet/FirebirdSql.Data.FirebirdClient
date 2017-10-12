﻿/*
 *  Firebird ADO.NET Data provider for .NET and Mono
 *
 *     The contents of this file are subject to the Initial
 *     Developer's Public License Version 1.0 (the "License");
 *     you may not use this file except in compliance with the
 *     License. You may obtain a copy of the License at
 *     http://www.firebirdsql.org/index.php?op=doc&id=idpl
 *
 *     Software distributed under the License is distributed on
 *     an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *     express or implied.  See the License for the specific
 *     language governing rights and limitations under the License.
 *
 *  Copyright (c) 2002, 2007 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

using System;
using System.Data;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.TestsBase;
using NUnit.Framework;

namespace FirebirdSql.Data.FirebirdClient.Tests
{
	[FbTestFixture(FbServerType.Default, false)]
	[FbTestFixture(FbServerType.Default, true)]
	[FbTestFixture(FbServerType.Embedded, default(bool))]
	public class FbDataAdapterTests : FbTestsBase
	{
		#region Constructors

		public FbDataAdapterTests(FbServerType serverType, bool compression)
			: base(serverType, compression)
		{ }

		#endregion

		#region Unit Tests

		[Test]
		public void FillTest()
		{
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand("select * from TEST", Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(100, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();
			transaction.Commit();
		}

		[Test]
		public void FillMultipleTest()
		{
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand("select * from TEST", Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds1 = new DataSet();
			DataSet ds2 = new DataSet();

			adapter.Fill(ds1, "TEST");
			adapter.Fill(ds2, "TEST");

			Assert.AreEqual(100, ds1.Tables["TEST"].Rows.Count, "Incorrect row count (ds1)");
			Assert.AreEqual(100, ds2.Tables["TEST"].Rows.Count, "Incorrect row count (ds2)");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();
			transaction.Commit();
		}

		[Test]
		public void FillMultipleWithImplicitTransactionTest()
		{
			FbCommand command = new FbCommand("select * from TEST", Connection);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds1 = new DataSet();
			DataSet ds2 = new DataSet();

			adapter.Fill(ds1, "TEST");
			adapter.Fill(ds2, "TEST");

			Assert.AreEqual(100, ds1.Tables["TEST"].Rows.Count, "Incorrect row count (ds1)");
			Assert.AreEqual(100, ds2.Tables["TEST"].Rows.Count, "Incorrect row count (ds2)");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();
		}

		[Test]
		public void InsertTest()
		{
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand("select * from TEST", Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(100, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			DataRow newRow = ds.Tables["TEST"].NewRow();

			newRow["int_field"] = 101;
			newRow["CHAR_FIELD"] = "ONE THOUSAND";
			newRow["VARCHAR_FIELD"] = ":;,.{}`+^*[]\\!|@#$%&/()?_-<>";
			newRow["BIGint_field"] = 100000;
			newRow["SMALLint_field"] = 100;
			newRow["DOUBLE_FIELD"] = 100.01;
			newRow["NUMERIC_FIELD"] = 100.01;
			newRow["DECIMAL_FIELD"] = 100.01;
			newRow["DATE_FIELD"] = new DateTime(100, 10, 10);
			newRow["TIME_FIELD"] = new TimeSpan(10, 10, 10);
			newRow["TIMESTAMP_FIELD"] = new DateTime(100, 10, 10, 10, 10, 10, 10);
			newRow["CLOB_FIELD"] = "ONE THOUSAND";

			ds.Tables["TEST"].Rows.Add(newRow);

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();
			transaction.Commit();
		}

		[Test]
		public void UpdateCharTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0]["CHAR_FIELD"] = "ONE THOUSAND";

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT char_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			string val = (string)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual("ONE THOUSAND", val.Trim(), "char_field has not correct value");
		}

		[Test]
		public void UpdateVarCharTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0]["VARCHAR_FIELD"] = "ONE VAR THOUSAND";

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT varchar_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			string val = (string)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual("ONE VAR THOUSAND", val.Trim(), "varchar_field has not correct value");
		}

		[Test]
		public void UpdateSmallIntTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0]["SMALLint_field"] = System.Int16.MaxValue;

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT smallint_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			short val = (short)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual(System.Int16.MaxValue, val, "smallint_field has not correct value");
		}

		[Test]
		public void UpdateBigIntTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0]["BIGINT_FIELD"] = System.Int32.MaxValue;

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT bigint_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			long val = (long)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual(System.Int32.MaxValue, val, "bigint_field has not correct value");
		}

		[Test]
		public void UpdateDoubleTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0]["DOUBLE_FIELD"] = System.Int32.MaxValue;

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT double_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			double val = (double)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual(System.Int32.MaxValue, val, "double_field has not correct value");
		}

		[Test]
		public void UpdateFloatTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0]["FLOAT_FIELD"] = (float)100.20;

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT float_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			float val = (float)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual((float)100.20, val, "double_field has not correct value");
		}

		[Test]
		public void UpdateNumericTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0]["NUMERIC_FIELD"] = System.Int32.MaxValue;

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT numeric_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			decimal val = (decimal)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual(System.Int32.MaxValue, val, "numeric_field has not correct value");
		}

		[Test]
		public void UpdateDecimalTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0]["DECIMAL_FIELD"] = System.Int32.MaxValue;

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT decimal_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			decimal val = (decimal)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual(System.Int32.MaxValue, val, "decimal_field has not correct value");
		}

		[Test]
		public void UpdateDateTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			DateTime dtValue = DateTime.Now;

			ds.Tables["TEST"].Rows[0]["DATE_FIELD"] = dtValue;

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT date_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			DateTime val = (DateTime)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual(dtValue.Day, val.Day, "date_field has not correct day");
			Assert.AreEqual(dtValue.Month, val.Month, "date_field has not correct month");
			Assert.AreEqual(dtValue.Year, val.Year, "date_field has not correct year");
		}

		[Test]
		public void UpdateTimeTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			TimeSpan dtValue = new TimeSpan(5, 6, 7);

			ds.Tables["TEST"].Rows[0]["TIME_FIELD"] = dtValue;

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT time_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			TimeSpan val = (TimeSpan)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual(dtValue.Hours, val.Hours, "time_field has not correct hour");
			Assert.AreEqual(dtValue.Minutes, val.Minutes, "time_field has not correct minute");
			Assert.AreEqual(dtValue.Seconds, val.Seconds, "time_field has not correct second");
		}

		[Test]
		public void UpdateTimeStampTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			DateTime dtValue = DateTime.Now;

			ds.Tables["TEST"].Rows[0]["TIMESTAMP_FIELD"] = dtValue;

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();

			transaction.Commit();

			transaction = Connection.BeginTransaction();

			sql = "SELECT timestamp_field FROM TEST WHERE int_field = @int_field";
			command = new FbCommand(sql, Connection, transaction);
			command.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			DateTime val = (DateTime)command.ExecuteScalar();

			transaction.Commit();

			Assert.AreEqual(dtValue.Day, val.Day, "timestamp_field has not correct day");
			Assert.AreEqual(dtValue.Month, val.Month, "timestamp_field has not correct month");
			Assert.AreEqual(dtValue.Year, val.Year, "timestamp_field has not correct year");
			Assert.AreEqual(dtValue.Hour, val.Hour, "timestamp_field has not correct hour");
			Assert.AreEqual(dtValue.Minute, val.Minute, "timestamp_field has not correct minute");
			Assert.AreEqual(dtValue.Second, val.Second, "timestamp_field has not correct second");
		}

		[Test]
		public void UpdateClobTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 1;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0]["CLOB_FIELD"] = "ONE THOUSAND";

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();
			transaction.Commit();
		}

		[Test]
		public void DeleteTest()
		{
			string sql = "select * from TEST where int_field = @int_field";
			FbTransaction transaction = Connection.BeginTransaction();
			FbCommand command = new FbCommand(sql, Connection, transaction);
			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			adapter.SelectCommand.Parameters.Add("@int_field", FbDbType.Integer).Value = 10;

			FbCommandBuilder builder = new FbCommandBuilder(adapter);

			DataSet ds = new DataSet();
			adapter.Fill(ds, "TEST");

			Assert.AreEqual(1, ds.Tables["TEST"].Rows.Count, "Incorrect row count");

			ds.Tables["TEST"].Rows[0].Delete();

			adapter.Update(ds, "TEST");

			adapter.Dispose();
			builder.Dispose();
			command.Dispose();
			transaction.Commit();
		}

		[Test]
		public void SubsequentDeletes()
		{
			string selectSql = "SELECT * FROM test";
			string deleteSql = "DELETE FROM test WHERE int_field = @id";

			// Set up conenction and select/delete commands
			FbConnection connection = new FbConnection(Connection.ConnectionString);
			FbCommand select = new FbCommand(selectSql, connection);
			FbCommand delete = new FbCommand(deleteSql, connection);
			delete.Parameters.Add("@id", FbDbType.Integer);
			delete.Parameters[0].SourceColumn = "INT_FIELD";

			// Set up the FbDataAdapter
			FbDataAdapter adapter = new FbDataAdapter(select);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
			adapter.DeleteCommand = delete;

			// Set up dataset
			DataSet ds = new DataSet();
			adapter.Fill(ds);

			// Delete one row
			ds.Tables[0].Rows[0].Delete();
			adapter.Update(ds);

			// Delete another row
			ds.Tables[0].Rows[0].Delete();
			adapter.Update(ds);

			// Delete another row
			ds.Tables[0].Rows[0].Delete();
			adapter.Update(ds);
		}

		[Test]
		[Ignore("Not supported")]
		public void MultipleResultsetTest()
		{
			FbCommand command = new FbCommand("", Connection);

			command.CommandText = "select * from test;";
			command.CommandText += "select int_field from test;";
			command.CommandText += "select int_field, char_field from test;";

			FbDataAdapter adapter = new FbDataAdapter(command);
			adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

			DataSet ds = new DataSet();

			adapter.Fill(ds);

			Assert.AreEqual(3, ds.Tables.Count, "Incorrect tables count");
		}

		#endregion
	}
}
