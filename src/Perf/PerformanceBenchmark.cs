using System.Diagnostics.Tracing;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Perf;

[Config(typeof(Config))]
public class PerformanceBenchmark
{
	class Config : ManualConfig
	{
		private const ClrTraceEventParser.Keywords EventParserKeywords =
			ClrTraceEventParser.Keywords.Exception
			| ClrTraceEventParser.Keywords.GC
			| ClrTraceEventParser.Keywords.Jit
			| ClrTraceEventParser.Keywords.JitTracing // for the inlining events
			| ClrTraceEventParser.Keywords.Loader
			| ClrTraceEventParser.Keywords.NGen;

		public Config()
		{
			AddJob(Job.ShortRun.WithRuntime(CoreRuntime.Core70));

			AddDiagnoser(MemoryDiagnoser.Default);

			AddDiagnoser(new EventPipeProfiler(providers: new[] {
				new EventPipeProvider(
					ClrTraceEventParser.ProviderName,
					EventLevel.Verbose,
					(long)EventParserKeywords
				)
			}));
		}
	}

	protected readonly string ConnectionString = (new FbConnectionStringBuilder()
	{
		Database = Path.Join(Path.GetTempPath(), "FirebirdSql.Data.FirebirdClient.Benchmark.fb50.fdb"),
		UserID = "SYSDBA",
		Password = "masterkey",
		ServerType = FbServerType.Embedded,
		ClientLibrary = Path.Join(Path.GetTempPath(), @"firebird-binaries\fb50\fbclient.dll"),
	}).ConnectionString;

	[Params(10_000)]
	public int Count { get; set; }

	[Params(
		"BIGINT",
		"CHAR(255) CHARACTER SET UTF8",
		"CHAR(255) CHARACTER SET OCTETS",
		"BLOB SUB_TYPE TEXT CHARACTER SET UTF8",
		"BLOB SUB_TYPE BINARY"
	)]
	public string DataType { get; set; }

	[GlobalSetup(Target = nameof(Fetch))]
	public void FetchGlobalSetup()
	{
		FbConnection.CreateDatabase(ConnectionString, 8192, false, true);
	}

	[GlobalCleanup]
	public void GlobalCleanup()
	{
		FbConnection.ClearAllPools();
		FbConnection.DropDatabase(ConnectionString);
	}

	[Benchmark]
	public void Fetch()
	{
		using var conn = new FbConnection(ConnectionString);
		conn.Open();

		using var cmd = conn.CreateCommand();
		cmd.CommandText = $@"
			EXECUTE BLOCK RETURNS (result {DataType}) AS
			DECLARE cnt INTEGER;
			BEGIN
				SELECT {GetFillExpression(DataType)} FROM rdb$database INTO result;
				cnt = {Count};
				WHILE (cnt > 0) DO
				BEGIN
					SUSPEND;
					cnt = cnt - 1;
				END
			END
		";

		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			_ = reader[0];
		}
	}
	private static string GetFillExpression(string dataType) =>
		dataType switch
		{
			{ } when dataType.StartsWith("BLOB") => $"LPAD('', 1023, '{dataType};')",
			{ } when dataType.StartsWith("CHAR") => $"LPAD('', 255, '{dataType};')",
			_ => "9223372036854775807" /* BIGINT */
		};
}