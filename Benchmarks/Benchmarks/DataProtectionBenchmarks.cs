using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace Benchmarks;

[MemoryDiagnoser]
public class DataProtectionBenchmarks
{
    private readonly IDataProtector _dataProtector;
    private readonly int _repeatCount;

    const string LoremIpsumData = """
        Lorem ipsum dolor sit amet, consectetur adipiscing elit.
        In sit amet libero in urna pretium ullamcorper sit amet at est.
        Morbi finibus dui non aliquam faucibus. Maecenas tempor viverra vulputate.
        Sed id luctus nibh. Etiam eu metus ligula.
    """;

    public DataProtectionBenchmarks()
    {
        _repeatCount = 100;

        var services = new ServiceCollection()
            .AddDataProtection()
            .Services.BuildServiceProvider();
        _dataProtector = services.GetDataProtector("SamplePurpose");
    }


    [Benchmark]
    public void Protect()
    {
        for (var i = 0; i < _repeatCount; i++)
        {
            _ = _dataProtector.Protect(LoremIpsumData);
        }
    }

    [Benchmark]
    public void Unprotect()
    {
        var protectedData = _dataProtector.Protect(LoremIpsumData);

        for (var i = 0; i < _repeatCount; i++)
        {
            _ = _dataProtector.Unprotect(protectedData);
        }
    }
}
