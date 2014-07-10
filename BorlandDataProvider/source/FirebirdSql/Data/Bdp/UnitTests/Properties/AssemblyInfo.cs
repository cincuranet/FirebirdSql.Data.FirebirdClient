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
 *  Copyright (c) 2004 Carlos Guzman Alvarez
 *  All Rights Reserved.
 */

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

//
// La informaci�n general de un ensamblado se controla mediante el siguiente 
// conjunto de atributos. Cambie estos atributos para modificar la informaci�n
// asociada con un ensamblado.
//
[assembly: AssemblyTitle("")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]		

//
// La informaci�n de versi�n de un ensamblado consta de los siguientes cuatro valores:
//
//      Versi�n principal
//      Versi�n secundaria 
//      Versi�n de compilaci�n
//      Revisi�n
//
// Puede especificar todos los valores o usar los valores predeterminados (n�mero de versi�n de compilaci�n y de revisi�n) 
// usando el s�mbolo '*' como se muestra a continuaci�n:

[assembly: AssemblyVersion("1.0.*")]

//
// Si desea firmar el ensamblado, debe especificar una clave para su uso. Consulte la documentaci�n de 
// Microsoft .NET Framework para obtener m�s informaci�n sobre la firma de ensamblados.
//
// Utilice los atributos siguientes para controlar qu� clave desea utilizar para firmar. 
//
// Notas: 
//   (*) Si no se especifica ninguna clave, el ensamblado no se firma.
//   (*) KeyName se refiere a una clave instalada en el Proveedor de servicios
//       de cifrado (CSP) en el equipo. KeyFile se refiere a un archivo que contiene
//       una clave.
//   (*) Si se especifican los valores KeyFile y KeyName, tendr� 
//       lugar el siguiente proceso:
//       (1) Si KeyName se puede encontrar en el CSP, se utilizar� dicha clave.
//       (2) Si KeyName y KeyFile no existen, se instalar� 
//           y utilizar� la clave de KeyFile en el CSP.
//   (*) Para crear KeyFile, puede ejecutar la utilidad sn.exe (Strong Name).
//       Cuando se especifica KeyFile, la ubicaci�n de KeyFile debe ser
//       relativa al directorio de resultados del proyecto, que es
//       %Directorio del proyecto%\obj\<configuraci�n>. Por ejemplo, si KeyFile
//       se encuentra en el directorio del proyecto, el atributo AssemblyKeyFile se especifica 
//       como [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Firma retardada es una opci�n avanzada; consulte la documentaci�n de
//       Microsoft .NET Framework para obtener m�s informaci�n.
//
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile("FirebirdSql.Data.Bdp.snk")]
[assembly: AssemblyKeyName("")]

//
// Use the attributes below to control the COM visibility of your assembly. By
// default the entire assembly is visible to COM. Setting ComVisible to false
// is the recommended default for your assembly. To then expose a class and interface
// to COM set ComVisible to true on each one. It is also recommended to add a
// Guid attribute.
//

[assembly: ComVisible(false)]
//[assembly: Guid("")]
//[assembly: TypeLibVersion(1, 0)]
