/*                 
 *            FirebirdSql.EntityFrameworkCore.Firebird
 *              https://www.firebirdsql.org/en/net-provider/ 
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
 *                  All Rights Reserved.
 */

using System;
using System.Text.RegularExpressions;

namespace Microsoft.EntityFrameworkCore.Storage.Internal
{
    public class ServerVersion
    {
       
        public ServerVersion(string versionStr)
        {
            var version = ReVersion.Matches(versionStr);
            if (version.Count > 0)
                Version = Version.Parse(version[0].Value);
			else
			{
				throw new InvalidOperationException($"Unable to determine server version from version string '{versionStr}'." +
					$"Supported versions:{string.Join(", ", SupportedVersions)} ");
			}
        }

	    private static readonly string[] SupportedVersions = { "2.1", "2.5", "3.0", "4.0" };

		internal Regex ReVersion = new Regex(@"\d+\.\d+\.?(?:\d+)?");

		public readonly Version Version;

        public bool SupportIdentityIncrement => Version.Major >= 3;

		public int ObjectLengthName => Version.Major >= 3 ? 64 : 31;
    }

}
