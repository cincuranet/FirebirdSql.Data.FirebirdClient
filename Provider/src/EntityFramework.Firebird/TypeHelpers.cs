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
 *  Copyright (c) 2008-2017 Jiri Cincura (jiri@cincura.net)
 *  All Rights Reserved.
 */

using System.Data.Entity.Core.Metadata.Edm;

namespace FirebirdSql.Data.EntityFramework6
{
	internal static class TypeHelpers
	{
		public static bool TryGetPrecision(TypeUsage tu, out byte precision)
		{
			precision = 0;
			if (tu.Facets.TryGetValue("Precision", false, out var f))
			{
				if (!f.IsUnbounded && f.Value != null)
				{
					precision = (byte)f.Value;
					return true;
				}
			}
			return false;
		}

		public static bool TryGetMaxLength(TypeUsage tu, out int maxLength)
		{
			maxLength = 0;
			if (tu.Facets.TryGetValue("MaxLength", false, out var f))
			{
				if (!f.IsUnbounded && f.Value != null)
				{
					maxLength = (int)f.Value;
					return true;
				}
			}
			return false;
		}

		public static bool TryGetScale(TypeUsage tu, out byte scale)
		{
			scale = 0;
			if (tu.Facets.TryGetValue("Scale", false, out var f))
			{
				if (!f.IsUnbounded && f.Value != null)
				{
					scale = (byte)f.Value;
					return true;
				}
			}
			return false;
		}
	}
}
