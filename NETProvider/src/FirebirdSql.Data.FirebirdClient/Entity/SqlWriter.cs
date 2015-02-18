/*
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
 *  Copyright (c) 2008-2014 Jiri Cincura (jiri@cincura.net)
 *  All Rights Reserved.
 */

#if (!(NET_35 && !ENTITY_FRAMEWORK))

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
#if (!EF_6)
using System.Data.Metadata.Edm;
using System.Data.Common.CommandTrees;
#else
#endif

#if (!EF_6)
namespace FirebirdSql.Data.Entity
#else
namespace FirebirdSql.Data.EntityFramework6.SqlGen
#endif
{
	/// <summary>
	/// This extends StringWriter primarily to add the ability to add an indent
	/// to each line that is written out.
	/// </summary>
	internal class SqlWriter : StringWriter
	{
		#region Fields

		// We start at -1, since the first select statement will increment it to 0.
		private int     indent              = -1;
		private bool    atBeginningOfLine   = true;

		#endregion

		#region Properties

		/// <summary>
		/// The number of tabs to be added at the beginning of each new line.
		/// </summary>
		internal int Indent
		{
			get { return this.indent; }
			set { this.indent = value; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// 
		/// </summary>
		/// <param name="b"></param>
		public SqlWriter(StringBuilder b) 
			: base(b, System.Globalization.CultureInfo.InvariantCulture)
		{
		}

		#endregion

		#region Methods

		public override void Write(string value)
		{
			if (value == Environment.NewLine)
			{
				base.WriteLine();
				this.atBeginningOfLine = true;
			}
			else
			{
				if (this.atBeginningOfLine)
				{
					if (indent > 0)
					{
						base.Write(new string('\t', indent));
					}
					this.atBeginningOfLine = false;
				}
				base.Write(value);
			}
		}

		public override void WriteLine()
		{
			base.WriteLine();
			this.atBeginningOfLine = true;
		}

		public override void WriteLine(string value)
		{
			Write(value);
			WriteLine();
		}

		#endregion
	}
}
#endif
