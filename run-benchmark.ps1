param(
	[Parameter(Mandatory=$True)]
	[ValidateSet('CommandBenchmark','PerformanceBenchmark')]
	$Benchmark = 'CommandBenchmark'
)

$ErrorActionPreference = 'Stop'

# Make a release build
dotnet build .\src\Perf\Perf.csproj --configuration 'Release'

# Run selected benchmark
& .\src\Perf\bin\Release\net8.0\Perf.exe --filter "*$($Benchmark)*"
