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
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Metadata
{
	public class FbPropertyAnnotations : RelationalPropertyAnnotations, IFbPropertyAnnotations
	{
		public FbPropertyAnnotations([NotNull] IProperty property)
			: base(property)
		{
		}

		protected FbPropertyAnnotations([NotNull] RelationalAnnotations annotations)
			: base(annotations)
		{
		}

		public virtual FbValueGenerationStrategy? ValueGenerationStrategy
		{
			get { return GetFbValueGenerationStrategy(fallbackToModel: true); }
			[param: CanBeNull]
			set { SetValueGenerationStrategy(value); }
		}

		public virtual FbValueGenerationStrategy? GetFbValueGenerationStrategy(bool fallbackToModel)
		{
			if (GetDefaultValue(false) != null
				|| GetDefaultValueSql(false) != null
				|| GetComputedColumnSql(false) != null)
			{
				return null;
			}

			var value = (FbValueGenerationStrategy?)Annotations.Metadata[FbAnnotationNames.ValueGenerationStrategy];

			if (value != null)
			{
				return value;
			}

			var relationalProperty = Property.Relational();
			if (!fallbackToModel
				|| Property.ValueGenerated != ValueGenerated.OnAdd
				|| relationalProperty.DefaultValue != null
				|| relationalProperty.DefaultValueSql != null
				|| relationalProperty.ComputedColumnSql != null)
			{
				return null;
			}

			var modelStrategy = Property.DeclaringEntityType.Model.Firebird().ValueGenerationStrategy;

			if (modelStrategy == FbValueGenerationStrategy.IdentityColumn && IsCompatibleIdentityColumn(Property.ClrType))
			{
				return FbValueGenerationStrategy.IdentityColumn;
			}

			if (modelStrategy == FbValueGenerationStrategy.ComputedColumn
				&& IsCompatibleIdentityColumn(Property.ClrType))
			{
				return FbValueGenerationStrategy.ComputedColumn;
			}

			return null;
		}

		protected virtual bool SetValueGenerationStrategy(FbValueGenerationStrategy? value)
		{
			if (value != null)
			{
				var propertyType = Property.ClrType;

				if (value == FbValueGenerationStrategy.IdentityColumn && !IsCompatibleIdentityColumn(propertyType))
				{
					if (ShouldThrowOnInvalidConfiguration)
					{
						throw new ArgumentException($"{Property.Name} {Property.DeclaringEntityType.DisplayName()} {propertyType.ShortDisplayName()}");
					}

					return false;
				}
			}

			if (!CanSetValueGenerationStrategy(value))
			{
				return false;
			}

			if (!ShouldThrowOnConflict
				&& ValueGenerationStrategy != value
				&& value != null)
			{
				ClearAllServerGeneratedValues();
			}

			return Annotations.SetAnnotation(FbAnnotationNames.ValueGenerationStrategy, value);
		}

		protected virtual bool CanSetValueGenerationStrategy(FbValueGenerationStrategy? value)
		{
			if (GetFbValueGenerationStrategy(fallbackToModel: false) == value)
			{
				return true;
			}

			if (!Annotations.CanSetAnnotation(FbAnnotationNames.ValueGenerationStrategy, value))
			{
				return false;
			}

			if (ShouldThrowOnConflict)
			{
				if (GetDefaultValue(false) != null)
				{
					throw new InvalidOperationException(RelationalStrings.ConflictingColumnServerGeneration(nameof(ValueGenerationStrategy), Property.Name, nameof(DefaultValue)));
				}
				if (GetDefaultValueSql(false) != null)
				{
					throw new InvalidOperationException(RelationalStrings.ConflictingColumnServerGeneration(nameof(ValueGenerationStrategy), Property.Name, nameof(DefaultValueSql)));
				}
				if (GetComputedColumnSql(false) != null)
				{
					throw new InvalidOperationException(RelationalStrings.ConflictingColumnServerGeneration(nameof(ValueGenerationStrategy), Property.Name, nameof(ComputedColumnSql)));
				}
			}
			else if (value != null && (!CanSetDefaultValue(null) || !CanSetDefaultValueSql(null) || !CanSetComputedColumnSql(null)))
			{
				return false;
			}

			return true;
		}

		protected override object GetDefaultValue(bool fallback)
		{
			if (fallback && ValueGenerationStrategy != null)
			{
				return null;
			}
			return base.GetDefaultValue(fallback);
		}

		protected override bool CanSetDefaultValue(object value)
		{
			if (ShouldThrowOnConflict)
			{
				if (ValueGenerationStrategy != null)
				{
					throw new InvalidOperationException(RelationalStrings.ConflictingColumnServerGeneration(nameof(DefaultValue), Property.Name, nameof(ValueGenerationStrategy)));
				}
			}
			else if (value != null && !CanSetValueGenerationStrategy(null))
			{
				return false;
			}

			return base.CanSetDefaultValue(value);
		}

		protected override string GetDefaultValueSql(bool fallback)
		{
			if (fallback && ValueGenerationStrategy != null)
			{
				return null;
			}

			return base.GetDefaultValueSql(fallback);
		}

		protected override bool CanSetDefaultValueSql(string value)
		{
			if (ShouldThrowOnConflict)
			{
				if (ValueGenerationStrategy != null)
				{
					throw new InvalidOperationException(RelationalStrings.ConflictingColumnServerGeneration(nameof(DefaultValueSql), Property.Name, nameof(ValueGenerationStrategy)));
				}
			}
			else if (value != null && !CanSetValueGenerationStrategy(null))
			{
				return false;
			}

			return base.CanSetDefaultValueSql(value);
		}

		protected override string GetComputedColumnSql(bool fallback)
		{
			if (fallback && ValueGenerationStrategy != null)
			{
				return null;
			}

			return base.GetComputedColumnSql(fallback);
		}

		protected override bool CanSetComputedColumnSql(string value)
		{
			if (ShouldThrowOnConflict)
			{
				if (ValueGenerationStrategy != null)
				{
					throw new InvalidOperationException(RelationalStrings.ConflictingColumnServerGeneration(nameof(ComputedColumnSql), Property.Name, nameof(ValueGenerationStrategy)));
				}
			}
			else if (value != null && !CanSetValueGenerationStrategy(null))
			{
				return false;
			}

			return base.CanSetComputedColumnSql(value);
		}

		protected override void ClearAllServerGeneratedValues()
		{
			SetValueGenerationStrategy(null); 
			base.ClearAllServerGeneratedValues();
		}

		private static bool IsCompatibleIdentityColumn(Type type)
		{
			return type.IsInteger() || type == typeof(DateTime);
		}

	}
}
