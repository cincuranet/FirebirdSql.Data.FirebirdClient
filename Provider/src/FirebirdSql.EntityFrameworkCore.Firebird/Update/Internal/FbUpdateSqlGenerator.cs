/*
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

//$Authors = Jiri Cincura (jiri@cincura.net), Rafael Almeida (ralms@ralms.net)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FirebirdSql.EntityFrameworkCore.Firebird.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Update.Internal
{
	public class FbUpdateSqlGenerator : UpdateSqlGenerator, IFbUpdateSqlGenerator
	{
		private readonly IRelationalTypeMapper _typeMapperRelational;
		private string commaAppend;

		public FbUpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies, IRelationalTypeMapper typeMapper)
			: base(dependencies)
		{
			_typeMapperRelational = typeMapper;
		}

		public ResultSetMapping AppendBlockInsertOperation(StringBuilder commandStringBuilder, StringBuilder executeParameters, IReadOnlyList<ModificationCommand> modificationCommands, int commandPosition)
		{
			commandStringBuilder.Clear();
			commaAppend = string.Empty;
			for (var i = 0; i < modificationCommands.Count; i++)
			{
				var name = modificationCommands[i].TableName;
				var schema = modificationCommands[i].Schema;
				var operations = modificationCommands[i].ColumnModifications;
				var writeOperations = operations.Where(o => o.IsWrite).ToArray();
				var readOperations = operations.Where(o => o.IsRead).ToArray();
				if (writeOperations.Any())
				{
					AppendBlockVariable(executeParameters, writeOperations);
				}
				AppendInsertCommandHeader(commandStringBuilder, name, schema, writeOperations);
				AppendValuesHeader(commandStringBuilder, writeOperations);
				AppendValuesInsert(commandStringBuilder, writeOperations);
				if (readOperations.Length > 0)
				{
					AppendInsertOutputClause(commandStringBuilder, readOperations, operations);
				}
				else if (readOperations.Length == 0)
				{
					AppendSelectAffectedCountCommand(commandStringBuilder, name, schema, commandPosition);
				}
			}

			return ResultSetMapping.NotLastInResultSet;
		}

		public ResultSetMapping AppendBlockUpdateOperation(StringBuilder commandStringBuilder, StringBuilder executeParameters, IReadOnlyList<ModificationCommand> modificationCommands, int commandPosition)
		{
			commandStringBuilder.Clear();
			commaAppend = string.Empty;
			for (var i = 0; i < modificationCommands.Count; i++)
			{
				var name = modificationCommands[i].TableName;
				var operations = modificationCommands[i].ColumnModifications;
				var writeOperations = operations.Where(o => o.IsWrite).ToArray();
				var conditionsOperations = operations.Where(o => o.IsCondition).ToArray();

				if (writeOperations.Any())
				{
					AppendBlockVariable(executeParameters, writeOperations);
				}

				commandStringBuilder.Append($"UPDATE {SqlGenerationHelper.DelimitIdentifier(name)} SET ")
									.AppendJoinUpadate(writeOperations, SqlGenerationHelper, (sb, o, helper) =>
									{
										if (o.IsWrite)
										{
											sb.Append(SqlGenerationHelper.DelimitIdentifier(o.ColumnName))
											  .Append(" = ")
											  .Append($":{o.ParameterName}");
										}
									});

				if (conditionsOperations.Any())
				{
					AppendBlockVariable(executeParameters, conditionsOperations);
				}

				AppendWhereClauseCustom(commandStringBuilder, conditionsOperations);
				commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
				AppendUpdateOrDeleteOutputClause(commandStringBuilder);
				commandStringBuilder.AppendLine("SUSPEND;");
			}
			return ResultSetMapping.NotLastInResultSet;
		}

		public ResultSetMapping AppendBlockDeleteOperation(StringBuilder commandStringBuilder, StringBuilder executeParameters, IReadOnlyList<ModificationCommand> modificationCommands, int commandPosition)
		{
			var name = modificationCommands[0].TableName;
			commaAppend = string.Empty;
			for (var i = 0; i < modificationCommands.Count; i++)
			{
				var operations = modificationCommands[i].ColumnModifications;
				var conditionsOperations = operations.Where(o => o.IsCondition).ToArray();
				if (conditionsOperations.Any())
				{
					AppendBlockVariable(executeParameters, conditionsOperations);
				}
				commandStringBuilder.Append("DELETE FROM ");
				commandStringBuilder.Append(SqlGenerationHelper.DelimitIdentifier(name));
				AppendWhereClauseCustom(commandStringBuilder, conditionsOperations);
				commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
				AppendUpdateOrDeleteOutputClause(commandStringBuilder);
				commandStringBuilder.AppendLine("SUSPEND;");
			}
			return ResultSetMapping.NotLastInResultSet;
		}

		private void AppendBlockVariable(StringBuilder commandStringBuilder, IReadOnlyList<ColumnModification> operations)
		{
			foreach (var column in operations)
			{
				var _type = GetDataType(column.Property);
				commandStringBuilder.Append(commaAppend);
				commandStringBuilder.Append($"{column.ParameterName}  {_type}=@{column.ParameterName}");
				commaAppend = ",";
			}
		}

		private void AppendValuesInsert(StringBuilder commandStringBuilder, IReadOnlyList<ColumnModification> operations)
		{
			if (operations.Count > 0)
			{
				commandStringBuilder.Append("(")
									.AppendJoin(operations, SqlGenerationHelper, (sb, o, helper) =>
									{
										if (o.IsWrite)
											sb.Append(":").Append(o.ParameterName);
									})
									.Append(")");
			}
		}

		private void AppendWhereClauseCustom(StringBuilder commandStringBuilder, ColumnModification[] columns)
		{
			if (columns.FirstOrDefault(p => p.IsCondition) == null)
				return;

			commandStringBuilder.Append(" WHERE ")
								.AppendJoin(columns, SqlGenerationHelper, (sb, o, helper) =>
								{
									if (o.IsCondition)
										sb.Append(SqlGenerationHelper.DelimitIdentifier(o.ColumnName))
										  .Append(" = ")
										  .Append($":{o.ParameterName}");
								}, " AND ");
		}

		private void AppendUpdateOrDeleteOutputClause(StringBuilder commandStringBuilder)
		{
			commandStringBuilder.AppendLine("IF (ROW_COUNT > 0) THEN")
								.AppendLine("   AffectedRows=AffectedRows+1;");
		}

		private void AppendInsertOutputClause(StringBuilder commandStringBuilder, IReadOnlyList<ColumnModification> operations, IReadOnlyList<ColumnModification> allOperations)
		{
			if (allOperations.Count > 0 && allOperations[0] == operations[0])
			{
				commandStringBuilder.AppendLine($" RETURNING {SqlGenerationHelper.DelimitIdentifier(operations.First().ColumnName)} INTO :AffectedRows;")
									.AppendLine("IF (ROW_COUNT > 0) THEN")
									.AppendLine("   SUSPEND;");
			}
		}

		protected override ResultSetMapping AppendSelectAffectedCountCommand(StringBuilder commandStringBuilder, string name, string schema, int commandPosition)
		{
			commandStringBuilder.AppendLine(" RETURNING ROW_COUNT INTO :AffectedRows;")
								.AppendLine("   SUSPEND;");

			return ResultSetMapping.LastInResultSet;
		}

		protected override void AppendRowsAffectedWhereCondition(StringBuilder commandStringBuilder, int expectedRowsAffected)
		{
			throw new NotImplementedException();
		}

		protected override void AppendIdentityWhereCondition(StringBuilder commandStringBuilder, ColumnModification columnModification)
		{
			throw new NotImplementedException();
		}

		private string GetDataType(IProperty property)
		{
			var typeName = property.Firebird().ColumnType;
			if (typeName == null)
			{
				var propertyDefault = property.FindPrincipal();
				typeName = propertyDefault?.Firebird().ColumnType;
				if (typeName == null)
				{
					if (property.ClrType == typeof(string))
					{
						typeName = _typeMapperRelational.StringMapper?.FindMapping(property.IsUnicode()
							?? propertyDefault?.IsUnicode()
							?? true, false, null).StoreType;
					}

					else if (property.ClrType == typeof(byte[]))
						typeName = _typeMapperRelational.ByteArrayMapper?.FindMapping(false, false, null).StoreType;
					else
						typeName = _typeMapperRelational.FindMapping(property.ClrType).StoreType;
				}
			}
			if (property.ClrType == typeof(byte[]) && typeName != null)
				return "BLOB SUB_TYPE BINARY";

			return typeName;
		}
	}
}
