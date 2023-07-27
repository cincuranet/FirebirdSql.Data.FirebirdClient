param(
	[Parameter(Mandatory=$True)]$Benchmark = 'CommandBenchmark'
)

$ErrorActionPreference = 'Stop'



dotnet build .\src\Perf\Perf.csproj --configuration 'Release'
& .\src\Perf\bin\Release\net7.0\Perf.exe --filter "*$($Benchmark)*"

