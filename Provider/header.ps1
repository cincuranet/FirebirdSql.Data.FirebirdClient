$LicenseHeader = @"
/*
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
"@

gci -Recurse -Filter *.cs | %{
	$content = gc $_.FullName -Encoding UTF8
	$newContent = @()
	
	$started = $false
	foreach ($line in $content) {
		if ($line.StartsWith('//$Authors')) {
			$started = $true
			$header = $LicenseHeader
			$line = $LicenseHeader + "`r`n`r`n" + $line
		}
		if ($started) {
			$newContent += $line
		}		
	}
	if (!$started) {
		return
	}
	sc $_.FullName $newContent -Encoding UTF8
}