/*
 *  .NET External Procedure Engine for Firebird
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
 *  Copyright (c) 2005 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

#ifndef RUNTIMEHOST_H
#define RUNTIMEHOST_H

using namespace std;
using namespace mscorlib;

namespace Firebird
{
	namespace CLRRuntimeHost
	{
		class ApplicationDomain;

		class RuntimeHost
		{
		public:
			RuntimeHost();
			~RuntimeHost();
			void Load();
			void Load(const WCHAR* runtimeVersion);
			void Start();
			void Stop();
			void Unload();
			ApplicationDomain* GetDefaultDomain();
			IAppDomainSetup* CreateDomainSetup();
			_Evidence* CreateDomainEvidence();
			ApplicationDomain* CreateDomain(const wstring domainName);
			ApplicationDomain* CreateDomain(const wstring domainName, _Evidence* domainEvidence);
			ApplicationDomain* CreateDomain(const wstring domainName, _Evidence* domainEvidence, IAppDomainSetup* domainSetup);
			void UnloadDomain(ApplicationDomain* domain);
			ICorRuntimeHost* GetHandle();
			bool IsLoaded();
			bool IsStarted();

		private:
			bool isLoaded;
			bool isStarted;
			ICorRuntimeHost *runtime;
		};
	}
}

#endif