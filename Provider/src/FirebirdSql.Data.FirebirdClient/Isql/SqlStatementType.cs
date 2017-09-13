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

//$Authors = Abel Eduardo Pereira, Jiri Cincura (jiri@cincura.net), Olivier Metod

using System;

namespace FirebirdSql.Data.Isql
{
	/// <summary>
	/// DSQL and ISQL statement types.
	/// </summary>
	[Serializable]
	public enum SqlStatementType
	{
		/// <summary>
		/// Represents the SQL statement: <b>ALTER CHARACTER SET</b>
		/// </summary>
		AlterCharacterSet,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER DATABASE</b>
		/// </summary>
		AlterDatabase,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER DOMAIN</b>
		/// </summary>
		AlterDomain,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER EXCEPTION</b>
		/// </summary>
		AlterException,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER FUNCTION</b>
		/// </summary>
		AlterFunction,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER INDEX</b>
		/// </summary>
		AlterIndex,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER PROCEDURE</b>
		/// </summary>
		AlterProcedure,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER ROLE</b>
		/// </summary>
		AlterRole,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER SEQUENCE</b>
		/// </summary>
		AlterSequence,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER TABLE</b>
		/// </summary>
		AlterTable,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER TRIGGER</b>
		/// </summary>
		AlterTrigger,

		/// <summary>
		/// Represents the SQL statement: <b>ALTER VIEW</b>
		/// </summary>
		AlterView,

		/// <summary>
		/// Represents the SQL statement: <b>CLOSE</b>
		/// </summary>
		Close,

		/// <summary>
		/// Represents the SQL statement: <b>COMMENT ON</b>
		/// </summary>
		CommentOn,

		/// <summary>
		/// Represents the SQL statement: <b>COMMIT</b>
		/// </summary>
		Commit,

		/// <summary>
		/// Represents the SQL statement: <b>CONNECT</b>
		/// </summary>
		Connect,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE COLLATION</b>
		/// </summary>
		CreateCollation,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE DATABASE</b>
		/// </summary>
		CreateDatabase,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE DOMAIN</b>
		/// </summary>
		CreateDomain,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE EXCEPTION</b>
		/// </summary>
		CreateException,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE FUNCTION</b>
		/// </summary>
		CreateFunction,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE GENERATOR</b>
		/// </summary>
		CreateGenerator,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE INDEX</b>
		/// </summary>
		CreateIndex,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE PROCEDURE</b>
		/// </summary>
		CreateProcedure,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE ROLE</b>
		/// </summary>
		CreateRole,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE SEQUENCE</b>
		/// </summary>
		CreateSequence,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE SHADOW</b>
		/// </summary>
		CreateShadow,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE TABLE</b>
		/// </summary>
		CreateTable,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE TRIGGER</b>
		/// </summary>
		CreateTrigger,

		/// <summary>
		/// Represents the SQL statement: <b>CREATE VIEW</b>
		/// </summary>
		CreateView,

		/// <summary>
		/// Represents the SQL statement: <b>DECLARE CURSOR</b>
		/// </summary>
		DeclareCursor,

		/// <summary>
		/// Represents the SQL statement: <b>DECLARE EXTERNAL FUNCTION</b>
		/// </summary>
		DeclareExternalFunction,

		/// <summary>
		/// Represents the SQL statement: <b>DECLARE FILTER</b>
		/// </summary>
		DeclareFilter,

		/// <summary>
		/// Represents the SQL statement: <b>DECLARE STATEMENT</b>
		/// </summary>
		DeclareStatement,

		/// <summary>
		/// Represents the SQL statement: <b>DECLARE TABLE</b>
		/// </summary>
		DeclareTable,

		/// <summary>
		/// Represents the SQL statement: <b>DELETE</b>
		/// </summary>
		Delete,

		/// <summary>
		/// Represents the SQL statement: <b>DESCRIBE</b>
		/// </summary>
		Describe,

		/// <summary>
		/// Represents the SQL statement: <b>DISCONNECT</b>
		/// </summary>
		Disconnect,

		/// <summary>
		/// Represents the SQL statement: <b>DROP COLLATION</b>
		/// </summary>
		DropCollation,

		/// <summary>
		/// Represents the SQL statement: <b>DROP DATABASE</b>
		/// </summary>
		DropDatabase,

		/// <summary>
		/// Represents the SQL statement: <b>DROP DOMAIN</b>
		/// </summary>
		DropDomain,

		/// <summary>
		/// Represents the SQL statement: <b>DROP EXCEPTION</b>
		/// </summary>
		DropException,

		/// <summary>
		/// Represents the SQL statement: <b>DROP EXTERNAL FUNCTION</b>
		/// </summary>
		DropExternalFunction,

		/// <summary>
		/// Represents the SQL statement: <b>DROP FUNCTION</b>
		/// </summary>
		DropFunction,

		/// <summary>
		/// Represents the SQL statement: <b>DROP FILTER</b>
		/// </summary>
		DropFilter,

		/// <summary>
		/// Represents the SQL statement: <b>DROP GENERATOR</b>
		/// </summary>
		DropGenerator,

		/// <summary>
		/// Represents the SQL statement: <b>DROP INDEX</b>
		/// </summary>
		DropIndex,

		/// <summary>
		/// Represents the SQL statement: <b>DROP PROCEDURE</b>
		/// </summary>
		DropProcedure,

		/// <summary>
		/// Represents the SQL statement: <b>DROP SEQUENCE</b>
		/// </summary>
		DropSequence,

		/// <summary>
		/// Represents the SQL statement: <b>DROP ROLE</b>
		/// </summary>
		DropRole,

		/// <summary>
		/// Represents the SQL statement: <b>DROP SHADOW</b>
		/// </summary>
		DropShadow,

		/// <summary>
		/// Represents the SQL statement: <b>DROP TABLE</b>
		/// </summary>
		DropTable,

		/// <summary>
		/// Represents the SQL statement: <b>DROP TRIGGER</b>
		/// </summary>
		DropTrigger,

		/// <summary>
		/// Represents the SQL statement: <b>DROP VIEW</b>
		/// </summary>
		DropView,

		/// <summary>
		/// Represents the SQL statement: <b>END DECLARE SECTION</b>
		/// </summary>
		EndDeclareSection,

		/// <summary>
		/// Represents the SQL statement: <b>EVENT INIT</b>
		/// </summary>
		EventInit,

		/// <summary>
		/// Represents the SQL statement: <b>EVENT WAIT</b>
		/// </summary>
		EventWait,

		/// <summary>
		/// Represents the SQL statement: <b>EXECUTE</b>
		/// </summary>
		Execute,

		/// <summary>
		/// Represents the SQL statement: <b>EXECUTE BLOCK</b>
		/// </summary>
		ExecuteBlock,

		/// <summary>
		/// Represents the SQL statement: <b>EXECUTE IMMEDIATE</b>
		/// </summary>
		ExecuteImmediate,

		/// <summary>
		/// Represents the SQL statement: <b>EXECUTE PROCEDURE</b>
		/// </summary>
		ExecuteProcedure,

		/// <summary>
		/// Represents the SQL statement: <b>FETCH</b>
		/// </summary>
		Fetch,

		/// <summary>
		/// Represents the SQL statement: <b>GRANT</b>
		/// </summary>
		Grant,

		/// <summary>
		/// Represents the SQL statement: <b>INSERT</b>
		/// </summary>
		Insert,

		/// <summary>
		/// Represents the SQL statement: <b>INSERT CURSOR</b>
		/// </summary>
		InsertCursor,

		/// <summary>
		/// Represents the SQL statement: <b>OPEN</b>
		/// </summary>
		Open,

		/// <summary>
		/// Represents the SQL statement: <b>PREPARE</b>
		/// </summary>
		Prepare,

		/// <summary>
		/// Represents the SQL statement: <b>RECREATE FUNCTION</b>
		/// </summary>
		RecreateFunction,

		/// <summary>
		/// Represents the SQL statement: <b>RECREATE PROCEDURE</b>
		/// </summary>
		RecreateProcedure,

		/// <summary>
		/// Represents the SQL statement: <b>RECREATE TABLE</b>
		/// </summary>
		RecreateTable,

		/// <summary>
		/// Represents the SQL statement: <b>RECREATE TRIGGER</b>
		/// </summary>
		RecreateTrigger,

		/// <summary>
		/// Represents the SQL statement: <b>RECREATE VIEW</b>
		/// </summary>
		RecreateView,

		/// <summary>
		/// Represents the SQL statement: <b>REVOKE</b>
		/// </summary>
		Revoke,

		/// <summary>
		/// Represents the SQL statement: <b>ROLLBACK</b>
		/// </summary>
		Rollback,

		/// <summary>
		/// Represents the SQL statement: <b>SELECT</b>
		/// </summary>
		Select,

		/// <summary>
		/// Represents the SQL statement: <b>SET AUTODDL</b>
		/// </summary>
		SetAutoDDL,

		/// <summary>
		/// Represents the SQL statement: <b>SET DATABASE</b>
		/// </summary>
		SetDatabase,

		/// <summary>
		/// Represents the SQL statement: <b>SET GENERATOR</b>
		/// </summary>
		SetGenerator,

		/// <summary>
		/// Represents the SQL statement: <b>SET NAMES</b>
		/// </summary>
		SetNames,

		/// <summary>
		/// Represents the SQL statement: <b>SET SQL DIALECT</b>
		/// </summary>
		SetSQLDialect,

		/// <summary>
		/// Represents the SQL statement: <b>SET STATISTICS</b>
		/// </summary>
		SetStatistics,

		/// <summary>
		/// Represents the SQL statement: <b>SET TRANSACTION</b>
		/// </summary>
		SetTransaction,

		/// <summary>
		/// Represents the SQL statement: <b>SHOW SQL DIALECT</b>
		/// </summary>
		ShowSQLDialect,

		/// <summary>
		/// Represents the SQL statement: <b>UPDATE</b>
		/// </summary>
		Update,

		/// <summary>
		/// Represents the SQL statement: <b>WHENEVER</b>
		/// </summary>
		Whenever,
	}
}
