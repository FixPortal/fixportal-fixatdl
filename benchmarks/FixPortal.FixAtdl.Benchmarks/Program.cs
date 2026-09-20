using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Benchmarks;

public static class Program
{
    public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 15)]
public class StrategyParsingBenchmarks
{
    private static readonly string FixtureRoot = Path.Join(AppContext.BaseDirectory, "Fixtures");
    private readonly StrategiesReader _reader = new();
    private byte[] _twap = null!;
    private byte[] _tzClock = null!;
    private byte[] _regionsEnums = null!;

    [GlobalSetup]
    public void Setup()
    {
        _twap = File.ReadAllBytes(Path.Join(FixtureRoot, "twap.xml"));
        _tzClock = File.ReadAllBytes(Path.Join(FixtureRoot, "RealWorld", "tz-clock.xml"));
        _regionsEnums = File.ReadAllBytes(Path.Join(FixtureRoot, "RealWorld", "regions-enums.xml"));
    }

    [Benchmark]
    public Strategies_t ParseTwap() => Parse(_twap);

    [Benchmark]
    public Strategies_t ParseTzClock15Strategies() => Parse(_tzClock);

    [Benchmark]
    public Strategies_t ParseRegionsEnums13Strategies() => Parse(_regionsEnums);

    private Strategies_t Parse(byte[] xml)
    {
        using var stream = new MemoryStream(xml, writable: false);
        return _reader.Load(stream);
    }
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 15)]
public class FixMessageBenchmarks
{
    private const char Soh = (char)0x01;
    private static readonly string RawMessage = string.Join(
        Soh,
        "8=FIX.4.4",
        "9=180",
        "35=D",
        "34=42",
        "49=BUY_SIDE",
        "56=BROKER",
        "52=20260828-14:00:00.000",
        "11=ORDER-000042",
        "55=GB00B03MLX29",
        "54=1",
        "38=125000",
        "40=2",
        "44=27.35",
        "59=0",
        "60=20260828-14:00:00.000",
        "21=1",
        "100=XLON",
        "167=CS",
        "207=XLON",
        "10=000",
        string.Empty
    );
    private FixMessage _message = null!;

    [GlobalSetup]
    public void Setup() => _message = new FixMessage(RawMessage);

    [Benchmark]
    public FixMessage Parse20Fields() => new(RawMessage);

    [Benchmark]
    public string Emit20Fields() => _message.ToFix();
}
