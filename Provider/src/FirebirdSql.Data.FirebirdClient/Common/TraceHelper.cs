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

//$Authors = Jiri Cincura (jiri@cincura.net)

using System;
using System.Diagnostics;
using System.Linq;

namespace FirebirdSql.Data.Common
{
	internal static class TraceHelper
	{
		public const string Name = "FirebirdSql.Data.FirebirdClient";
		public const string ConditionalSymbol = "TRACE";

		static TraceSource _instance;

		static TraceHelper()
		{
			_instance = new TraceSource(Name, SourceLevels.All);
		}

		public static void Trace(TraceEventType eventType, string message)
		{
			_instance.TraceEvent(eventType, default(int), message);
			_instance.Flush();
		}

		public static bool HasListeners
		{
			get { return _instance.Listeners.Count > 0; }
		}
	}
}
