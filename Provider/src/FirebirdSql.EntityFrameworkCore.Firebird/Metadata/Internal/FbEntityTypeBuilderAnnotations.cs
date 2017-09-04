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

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Metadata.Internal
{
	public class FbEntityTypeBuilderAnnotations : FbEntityTypeAnnotations
	{
		public FbEntityTypeBuilderAnnotations(
			[NotNull] InternalEntityTypeBuilder internalBuilder, ConfigurationSource configurationSource)
			: base(new RelationalAnnotationsBuilder(internalBuilder, configurationSource))
		{
		}

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		public virtual bool ToSchema([CanBeNull] string name)
		{
			return SetSchema(Check.NullButNotEmpty(name, nameof(name)));
		}

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		public virtual bool ToTable([CanBeNull] string name)
		{
			return SetTableName(Check.NullButNotEmpty(name, nameof(name)));
		}

		/// <summary>
		///     This API supports the Entity Framework Core infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		public virtual bool ToTable([CanBeNull] string name, [CanBeNull] string schema)
		{
			var originalTable = TableName;
			if (!SetTableName(Check.NullButNotEmpty(name, nameof(name))))
				return false;

			if (!SetSchema(Check.NullButNotEmpty(schema, nameof(schema))))
			{
				SetTableName(originalTable);
				return false;
			}

			return true;
		}
	}
}
