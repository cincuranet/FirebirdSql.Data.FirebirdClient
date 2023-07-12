#
# FirebirdSql.Data.FirebirdClient Tasks for Invoke-Build
#
# Requires Invoke-Build -- https://github.com/nightroman/Invoke-Build
#
#   dotnet tool install --global ib
#

param(
    $Configuration = 'Debug',
    $VersionSuffix = $null
)


#
# Globals
#

$baseDir = Split-Path -Parent $PSCommandPath
$outDir = "$baseDir\out"
$version = ''

$solutionFile = "$baseDir\src\NETProvider.sln"


#
# Tasks
#

task Clean {
    # Remove output folder
    Remove-Item $outDir -Recurse -Force -ErrorAction SilentlyContinue
	mkdir $outDir | Out-Null

    # Remove binaries + nuget packages
    Exec { dotnet msbuild /t:Clean /p:Configuration=$Configuration /p:ContinuousIntegrationBuild=true $solutionFile /v:m /m }

    # Remove nuget packages
    Get-ChildItem "$baseDir\src\*\bin\$Configuration" -Include '*.nupkg','*.snupkg' -Recurse | 
        Remove-Item -Force -ErrorAction SilentlyContinue
}

task Build Clean, {
    # This sometimes fails on CI (call without Exec = do not check for exit code)
    dotnet msbuild /t:Restore /p:Configuration=$Configuration /p:ContinuousIntegrationBuild=true $solutionFile /v:m /m

    Exec { dotnet msbuild /t:Restore /p:Configuration=$Configuration /p:ContinuousIntegrationBuild=true $solutionFile /v:m /m }

    # Build binaries + nuget packages
    Exec { dotnet msbuild /t:Build /p:Configuration=$Configuration /p:ContinuousIntegrationBuild=true $solutionFile /v:m /m /p:VERSIONSUFFIX=$VersionSuffix }

    # Copy nuget packages to output folder
    Get-ChildItem "$baseDir\src\*\bin\$Configuration" -Include '*.nupkg','*.snupkg' -Recurse | 
        Copy-Item -Destination $outDir
}


#
# Default task
#

task . Build
