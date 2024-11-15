param(
	[Parameter(Mandatory=$True)]$Configuration)

$ErrorActionPreference = 'Stop'

$baseDir = Split-Path -Parent $PSCommandPath

. "$baseDir\include.ps1"

$outDir = "$baseDir\out"
$versionProvider = ''
$versionEFCore = ''

function Clean() {
	if (Test-Path $outDir) {
		rm -Force -Recurse $outDir
	}
	mkdir $outDir | Out-Null
}

function Build() {
	function b($target, $check=$True) {
		dotnet msbuild /t:$target /p:Configuration=$Configuration /p:ContinuousIntegrationBuild=true "$baseDir\src\NETProvider.sln" /v:m /m
		if ($check) {
			Check-ExitCode
		}
	}
	b 'Clean'
	# this sometimes fails on CI
	b 'Restore' $False
	b 'Restore'
	b 'Build'
}

function Versions() {
	function v($file) {
		return (Get-Item $file).VersionInfo.ProductVersion -replace '(\d+)\.(\d+)\.(\d+)(-[a-z0-9]+)?.*','$1.$2.$3$4'
	}
	$script:versionProvider = v $baseDir\src\FirebirdSql.Data.FirebirdClient\bin\$Configuration\net8.0\FirebirdSql.Data.FirebirdClient.dll
	$script:versionEFCore = v $baseDir\src\FirebirdSql.EntityFrameworkCore.Firebird\bin\$Configuration\net8.0\FirebirdSql.EntityFrameworkCore.Firebird.dll
}

function NuGets() {
	cp $baseDir\src\FirebirdSql.Data.FirebirdClient\bin\$Configuration\FirebirdSql.Data.FirebirdClient.$versionProvider.nupkg $outDir
	cp $baseDir\src\FirebirdSql.EntityFrameworkCore.Firebird\bin\$Configuration\FirebirdSql.EntityFrameworkCore.Firebird.$versionEFCore.nupkg $outDir

	cp $baseDir\src\FirebirdSql.Data.FirebirdClient\bin\$Configuration\FirebirdSql.Data.FirebirdClient.$versionProvider.snupkg $outDir
	cp $baseDir\src\FirebirdSql.EntityFrameworkCore.Firebird\bin\$Configuration\FirebirdSql.EntityFrameworkCore.Firebird.$versionEFCore.snupkg $outDir
}

Clean
Build
Versions
NuGets
