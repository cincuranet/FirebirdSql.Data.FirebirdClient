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
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Internal;
using FirebirdSql.Data.FirebirdClient;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace FirebirdSql.EntityFrameworkCore.Firebird.Update.Internal
{
	public class FbModificationCommandBatch : AffectedCountModificationCommandBatch
	{
		private const int MaxParameterCount = 1000;
		private const int MaxRowCount = 256;
		private int _countParameter = 1;
		private readonly int _maxBatchSize;
		private readonly IRelationalCommandBuilderFactory _commandBuilderFactory;
		private readonly IRelationalValueBufferFactoryFactory _valueBufferFactory;
		private readonly List<ModificationCommand> _blockInsertCommands;
		private readonly List<ModificationCommand> _blockUpdateCommands;
		private readonly List<ModificationCommand> _blockDeleteCommands;
		private readonly StringBuilder _executeParameters;
		private string _seperator;

		public FbModificationCommandBatch(IRelationalCommandBuilderFactory commandBuilderFactory, ISqlGenerationHelper sqlGenerationHelper, IFbUpdateSqlGenerator updateSqlGenerator, IRelationalValueBufferFactoryFactory valueBufferFactoryFactory, int? maxBatchSize)
			: base(commandBuilderFactory, sqlGenerationHelper, updateSqlGenerator, valueBufferFactoryFactory)
		{
			if (maxBatchSize.HasValue && maxBatchSize.Value <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxBatchSize), RelationalStrings.InvalidMaxBatchSize);

			_maxBatchSize = Math.Min(maxBatchSize ?? int.MaxValue, MaxRowCount);
			_commandBuilderFactory = commandBuilderFactory;
			_valueBufferFactory = valueBufferFactoryFactory;
			_executeParameters = new StringBuilder();
			_seperator = string.Empty;
			_blockInsertCommands = new List<ModificationCommand>();
			_blockUpdateCommands = new List<ModificationCommand>();
			_blockDeleteCommands = new List<ModificationCommand>();
		}

		protected new virtual IFbUpdateSqlGenerator UpdateSqlGenerator()
		{
			return (IFbUpdateSqlGenerator)base.UpdateSqlGenerator;
		}

		protected override bool CanAddCommand(ModificationCommand modificationCommand)
		{
			if (ModificationCommands.Count >= _maxBatchSize)
				return false;

			var additionalParameterCount = CountParameters(modificationCommand);
			if (_countParameter + additionalParameterCount >= MaxParameterCount)
				return false;

			_countParameter += additionalParameterCount;
			return true;
		}

		protected override bool IsCommandTextValid() => true;

		protected override int GetParameterCount() => _countParameter;

		private static int CountParameters(ModificationCommand modificationCommand)
		{
			var parameterCount = 0;
			foreach (var columnModification in modificationCommand.ColumnModifications)
			{
				if (columnModification.UseCurrentValueParameter)
					parameterCount++;

				if (columnModification.UseOriginalValueParameter)
					parameterCount++;
			}
			return parameterCount;
		}

		protected override void ResetCommandText()
		{
			base.ResetCommandText();
			_executeParameters.Clear();
			_blockInsertCommands.Clear();
			_blockUpdateCommands.Clear();
			_blockDeleteCommands.Clear();
		}

		protected override string GetCommandText()
		{
			var sbCommands = new StringBuilder();
			var sbExecuteBlock = new StringBuilder();
			_executeParameters.Clear();

			//Commands Insert/Update/Delete
			sbCommands.AppendLine(base.GetCommandText());
			sbCommands.Append(GetBlockInsertCommandText(ModificationCommands.Count));
			sbCommands.Append(GetBlockUpdateCommandText(ModificationCommands.Count));
			sbCommands.Append(GetBlockDeleteCommandText(ModificationCommands.Count));

			//Execute Block
			var parameters = _executeParameters.ToString();
			sbExecuteBlock.Append("EXECUTE BLOCK ");
			if (parameters.Length > 0)
			{
				sbExecuteBlock.Append("( ");
				sbExecuteBlock.Append(parameters);
				sbExecuteBlock.Append(") ");
			}
			sbExecuteBlock.AppendLine("RETURNS (AffectedRows BIGINT) AS BEGIN");
			sbExecuteBlock.AppendLine("AffectedRows=0;");
			sbExecuteBlock.Append(sbCommands);
			sbExecuteBlock.AppendLine("END;");
			return sbExecuteBlock.ToString();
		}

		private string GetBlockDeleteCommandText(int lastIndex)
		{
			if (_blockDeleteCommands.Count == 0)
				return string.Empty;

			var stringBuilder = new StringBuilder();
			var headStringBuilder = new StringBuilder();
			var resultSetMapping = UpdateSqlGenerator().AppendBlockDeleteOperation(stringBuilder, _executeParameters, _blockDeleteCommands, lastIndex - _blockDeleteCommands.Count);
			for (var i = lastIndex - _blockDeleteCommands.Count; i < lastIndex; i++)
				CommandResultSet[i] = resultSetMapping;

			if (resultSetMapping != ResultSetMapping.NoResultSet)
				CommandResultSet[lastIndex - 1] = ResultSetMapping.LastInResultSet;

			var executeParameters = headStringBuilder.ToString();
			if (executeParameters.Length > 0)
			{
				_executeParameters.Append(_seperator);
				_executeParameters.Append(executeParameters);
				_seperator = ",";
			}

			return stringBuilder.ToString();
		}

		private string GetBlockUpdateCommandText(int lastIndex)
		{
			if (_blockUpdateCommands.Count == 0)
				return string.Empty;

			var stringBuilder = new StringBuilder();
			var headStringBuilder = new StringBuilder();
			var resultSetMapping = UpdateSqlGenerator().AppendBlockUpdateOperation(stringBuilder, headStringBuilder, _blockUpdateCommands, lastIndex - _blockUpdateCommands.Count);

			for (var i = lastIndex - _blockUpdateCommands.Count; i < lastIndex; i++)
				CommandResultSet[i] = resultSetMapping;

			if (resultSetMapping != ResultSetMapping.NoResultSet)
				CommandResultSet[lastIndex - 1] = ResultSetMapping.LastInResultSet;

			var executeParameters = headStringBuilder.ToString();
			if (executeParameters.Length > 0)
			{
				_executeParameters.Append(_seperator);
				_executeParameters.Append(executeParameters);
				_seperator = ",";
			}

			return stringBuilder.ToString();
		}

		private string GetBlockInsertCommandText(int lastIndex)
		{
			if (_blockInsertCommands.Count == 0)
				return string.Empty;

			var stringBuilder = new StringBuilder();
			var headStringBuilder = new StringBuilder();
			var resultSetMapping = UpdateSqlGenerator().AppendBlockInsertOperation(stringBuilder, headStringBuilder, _blockInsertCommands, lastIndex - _blockInsertCommands.Count);

			for (var i = lastIndex - _blockInsertCommands.Count; i < lastIndex; i++)
				CommandResultSet[i] = resultSetMapping;

			if (resultSetMapping != ResultSetMapping.NoResultSet)
				CommandResultSet[lastIndex - 1] = ResultSetMapping.LastInResultSet;

			var executeParameters = headStringBuilder.ToString();
			if (executeParameters.Length > 0)
			{
				_executeParameters.Append(_seperator);
				_executeParameters.Append(executeParameters);
				_seperator = ",";
			}

			return stringBuilder.ToString();
		}

		protected override void UpdateCachedCommandText(int commandPosition)
		{
			var newModificationCommand = ModificationCommands[commandPosition];

			if (newModificationCommand.EntityState == EntityState.Added)
			{
				if (_blockInsertCommands.Count > 0 && !CanBeInsertedInSameStatement(_blockInsertCommands[0], newModificationCommand))
				{
					CachedCommandText.Append(GetBlockInsertCommandText(commandPosition));
					_blockInsertCommands.Clear();
				}
				_blockInsertCommands.Add(newModificationCommand);
				LastCachedCommandIndex = commandPosition;
			}
			else if (newModificationCommand.EntityState == EntityState.Modified)
			{
				if (_blockUpdateCommands.Count > 0 && !CanBeUpdateInSameStatement(_blockUpdateCommands[0], newModificationCommand))
				{
					CachedCommandText.Append(GetBlockUpdateCommandText(commandPosition));
					_blockUpdateCommands.Clear();
				}
				_blockUpdateCommands.Add(newModificationCommand);
				LastCachedCommandIndex = commandPosition;
			}
			else if (newModificationCommand.EntityState == EntityState.Deleted)
			{
				if (_blockDeleteCommands.Count > 0 && !CanBeDeleteInSameStatement(_blockDeleteCommands[0], newModificationCommand))
				{
					CachedCommandText.Append(GetBlockDeleteCommandText(commandPosition));
					_blockDeleteCommands.Clear();
				}
				_blockDeleteCommands.Add(newModificationCommand);
				LastCachedCommandIndex = commandPosition;
			}
			else
			{
				CachedCommandText.Append(GetBlockInsertCommandText(commandPosition));
				_blockInsertCommands.Clear();
				base.UpdateCachedCommandText(commandPosition);
			}
		}

		private static bool CanBeDeleteInSameStatement(ModificationCommand firstCommand, ModificationCommand secondCommand)
		{
			return string.Equals(firstCommand.TableName, secondCommand.TableName, StringComparison.Ordinal)
				   && firstCommand.ColumnModifications.Where(o => o.IsWrite)
								  .Select(o => o.ColumnName)
								  .SequenceEqual(secondCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName))
				   && firstCommand.ColumnModifications.Where(o => o.IsRead)
								  .Select(o => o.ColumnName)
								  .SequenceEqual(secondCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName));
		}

		private static bool CanBeUpdateInSameStatement(ModificationCommand firstCommand, ModificationCommand secondCommand)
		{
			return string.Equals(firstCommand.TableName, secondCommand.TableName, StringComparison.Ordinal)
				   && firstCommand.ColumnModifications.Where(o => o.IsWrite)
								  .Select(o => o.ColumnName)
								  .SequenceEqual(secondCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName))
				   && firstCommand.ColumnModifications.Where(o => o.IsRead)
								  .Select(o => o.ColumnName)
								  .SequenceEqual(secondCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName));
		}

		private static bool CanBeInsertedInSameStatement(ModificationCommand firstCommand, ModificationCommand secondCommand)
		{
			return string.Equals(firstCommand.TableName, secondCommand.TableName, StringComparison.Ordinal)
				   && firstCommand.ColumnModifications.Where(o => o.IsWrite)
								  .Select(o => o.ColumnName)
								  .SequenceEqual(secondCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName))
				   && firstCommand.ColumnModifications.Where(o => o.IsRead)
								  .Select(o => o.ColumnName)
								  .SequenceEqual(secondCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName));
		}

		protected override void Consume(RelationalDataReader relationalReader)
		{
			if (relationalReader == null)
			{
				throw new ArgumentNullException(nameof(relationalReader));
			}

			var dataReader = (FbDataReader)relationalReader.DbDataReader;
			var commandIndex = 0;
			try
			{
				for (; ; )
				{
					while (commandIndex < CommandResultSet.Count && CommandResultSet[commandIndex] == ResultSetMapping.NoResultSet)
						commandIndex++;

					var propragation = commandIndex;
					while (propragation < ModificationCommands.Count && !ModificationCommands[propragation].RequiresResultPropagation)
						propragation++;

					while (commandIndex < propragation)
					{
						commandIndex++;
						if (!dataReader.Read())
							throw new DbUpdateConcurrencyException(RelationalStrings.UpdateConcurrencyException(1, 0), ModificationCommands[commandIndex].Entries);
					}
					//check if you've gone through all notifications
					if (propragation == ModificationCommands.Count)
						break;

					var modifications = ModificationCommands[commandIndex];
					if (!relationalReader.Read())
						throw new DbUpdateConcurrencyException(RelationalStrings.UpdateConcurrencyException(1, 0), modifications.Entries);

					var bufferFactory = CreateValueBufferFactory(modifications.ColumnModifications);
					modifications.PropagateResults(bufferFactory.Create(dataReader));
					dataReader.NextResult();
					commandIndex++;
				}
			}
			catch (DbUpdateException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new DbUpdateException(RelationalStrings.UpdateStoreException, ex, ModificationCommands[commandIndex].Entries);
			}
		}

		protected override Task ConsumeAsync(RelationalDataReader reader, CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.Run(() => Consume(reader), cancellationToken);
		}
	}
}
