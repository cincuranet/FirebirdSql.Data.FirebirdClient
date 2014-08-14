/*
 *  Firebird BDP - Borland Data provider Firebird
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
 *  Copyright (c) 2004-2006 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

using System;
using FirebirdSql.Data.Common;

namespace FirebirdSql.Data.Bdp
{
	internal class ClientFactory
	{
		public static IDatabase CreateDatabase(int serverType)
		{
			switch (serverType)
			{
				case 0:
					// C# Client
					return new FirebirdSql.Data.Gds.GdsDatabase();
				
				case 1:
					// PInvoke Client
					return new FirebirdSql.Data.Embedded.FesDatabase();

				default:					
					throw new NotSupportedException("Specified server type is not correct.");
			}
		}
	}
}
