/*                 
 *            FirebirdSql.EntityFrameworkCore.Firebird
 *     
 *              https://www.firebirdsql.org/en/net-provider/ 
 *              
 *     Permission to use, copy, modify, and distribute this software and its
 *     documentation for any purpose, without fee, and without a written
 *     agreement is hereby granted, provided that the above copyright notice
 *     and this paragraph and the following two paragraphs appear in all copies. 
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
 *      Credits: Rafael Almeida (ralms@ralms.net)
 *                              Sergipe-Brazil
 *
 *
 *                              
 *                  All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Internal;
using FirebirdSql.Data.FirebirdClient;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.EntityFrameworkCore.Update.Internal
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

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		public FbModificationCommandBatch(
			[NotNull] IRelationalCommandBuilderFactory commandBuilderFactory,
			[NotNull] ISqlGenerationHelper sqlGenerationHelper, 
			[NotNull] IFbUpdateSqlGenerator updateSqlGenerator,
			[NotNull] IRelationalValueBufferFactoryFactory valueBufferFactoryFactory,
			[CanBeNull] int? maxBatchSize)
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

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		protected new virtual IFbUpdateSqlGenerator UpdateSqlGenerator()
		{
			return (IFbUpdateSqlGenerator) base.UpdateSqlGenerator;
		}

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
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

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		protected override bool IsCommandTextValid()
		{
			return true;
		}


		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		protected override int GetParameterCount()
		{
			return _countParameter;
		}

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

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		protected override void ResetCommandText()
		{
			base.ResetCommandText();
			_blockInsertCommands.Clear();
			_blockUpdateCommands.Clear();
			_blockDeleteCommands.Clear();
		}

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
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
			sbExecuteBlock.AppendLine($"RETURNS (AffectedRows BIGINT) AS BEGIN");
			sbExecuteBlock.Append("AffectedRows=0;");
			sbExecuteBlock.Append(sbCommands);
			sbExecuteBlock.AppendLine("END;");
			return sbExecuteBlock.ToString().Trim();
		}

		private string GetBlockDeleteCommandText(int lastIndex)
		{
			if (_blockDeleteCommands.Count == 0)
				return string.Empty;


			var stringBuilder = new StringBuilder();
			var resultSetMapping = UpdateSqlGenerator()
				.AppendBlockDeleteOperation(stringBuilder, _blockDeleteCommands, lastIndex - _blockDeleteCommands.Count);
			for (var i = lastIndex - _blockDeleteCommands.Count; i < lastIndex; i++)
				CommandResultSet[i] = resultSetMapping;

			if (resultSetMapping != ResultSetMapping.NoResultSet)
				CommandResultSet[lastIndex - 1] = ResultSetMapping.LastInResultSet;

			return stringBuilder.ToString();
		}

		private string GetBlockUpdateCommandText(int lastIndex)
		{
			if (_blockUpdateCommands.Count == 0)
				return string.Empty;


			var stringBuilder = new StringBuilder();
			var headStringBuilder = new StringBuilder();
			var resultSetMapping = UpdateSqlGenerator().AppendBlockUpdateOperation(stringBuilder, headStringBuilder,
				_blockUpdateCommands, lastIndex - _blockUpdateCommands.Count);
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
			var resultSetMapping = UpdateSqlGenerator().AppendBlockInsertOperation(stringBuilder, headStringBuilder,
				_blockInsertCommands, lastIndex - _blockInsertCommands.Count);
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

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		protected override void UpdateCachedCommandText(int commandPosition)
		{
			var newModificationCommand = ModificationCommands[commandPosition];

			if (newModificationCommand.EntityState == EntityState.Added)
			{
				if (_blockInsertCommands.Count > 0
				    && !CanBeInsertedInSameStatement(_blockInsertCommands[0], newModificationCommand))
				{
					CachedCommandText.Append(GetBlockInsertCommandText(commandPosition));
					_blockInsertCommands.Clear();
				}
				_blockInsertCommands.Add(newModificationCommand);
				LastCachedCommandIndex = commandPosition;
			}
			else if (newModificationCommand.EntityState == EntityState.Modified)
			{
				if (_blockUpdateCommands.Count > 0
				    && !CanBeUpdateInSameStatement(_blockUpdateCommands[0], newModificationCommand))
				{
					CachedCommandText.Append(GetBlockUpdateCommandText(commandPosition));
					_blockUpdateCommands.Clear();
				}
				_blockUpdateCommands.Add(newModificationCommand);
				LastCachedCommandIndex = commandPosition;
			}
			else if (newModificationCommand.EntityState == EntityState.Deleted)
			{
				if (_blockDeleteCommands.Count > 0
				    && !CanBeDeleteInSameStatement(_blockDeleteCommands[0], newModificationCommand))
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
			       && firstCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName).SequenceEqual(
				       secondCommand.ColumnModifications.Where(o => o.IsWrite).Select(o => o.ColumnName))
			       && firstCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName).SequenceEqual(
				       secondCommand.ColumnModifications.Where(o => o.IsRead).Select(o => o.ColumnName));
		}


		///// <summary>
		///// Make the Datareader consummation
		///// </summary>
		///// <param name = "reader" ></ param >
		protected override void Consume(RelationalDataReader relationalReader)
		{
			if (relationalReader == null)
			{
				throw new ArgumentNullException(nameof(relationalReader));
			}
			//Cast FbDataReader
			var dataReader = (FbDataReader) relationalReader.DbDataReader;
			var commandIndex = 0;
			try
			{
				for (;;)
				{
					while (commandIndex < CommandResultSet.Count
					       && CommandResultSet[commandIndex] == ResultSetMapping.NoResultSet)
						commandIndex++;
					var propragation = commandIndex;
					while (propragation < ModificationCommands.Count &&
					       !ModificationCommands[propragation].RequiresResultPropagation)
						propragation++;

					while (commandIndex < propragation)
					{
						commandIndex++;
						if (!dataReader.Read())
							throw new DbUpdateConcurrencyException(
								RelationalStrings.UpdateConcurrencyException(1, 0),
								ModificationCommands[commandIndex].Entries
							);
					}
					//check if you've gone through all notifications
					if (propragation == ModificationCommands.Count)
						break;

					var modifications = ModificationCommands[commandIndex++];
					if (!relationalReader.Read())
						throw new DbUpdateConcurrencyException(
							RelationalStrings.UpdateConcurrencyException(1, 0),
							modifications.Entries);
					var bufferFactory = CreateValueBufferFactory(modifications.ColumnModifications);
					modifications.PropagateResults(bufferFactory.Create(dataReader));
					dataReader.NextResult();
				}
			}
			catch (DbUpdateException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new DbUpdateException(
					RelationalStrings.UpdateStoreException,
					ex,
					ModificationCommands[commandIndex].Entries);
			}
		}

		/// <summary>
		///     Method Async for Propagation DataReader
		/// </summary>
		/// <param name="reader"></param>
		/// < param name="cancellationToken"></param>
		/// <returns></returns>
		protected override Task ConsumeAsync(RelationalDataReader reader,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.Run(() => Consume(reader));
		}
	}
}
