using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class DataProtectionBenchmarks
    {
        private IDataProtector _dataProtector;
        const string AntiforgeryTokenSample = "CfDJ8H5oH_fp1QNBmvs-OWXxsVoV30hrXeI4-PI4p1VZytjsgd0DTstMdtTZbFtm2dKHvsBlDCv7TiEWKztZf8fb48pUgBgUE2SeYV3eOUXvSfNWU0D8SmHLy5KEnwKKkZKqudDhCnjQSIU7mhDliJJN1e4";

        string _protectedData;

        [GlobalSetup]
        public void Setup()
        {
            // I am resolving the data protector from ServiceProvider, because it is an internal type.
            var services = new ServiceCollection()
                .AddDataProtection()
                .Services.BuildServiceProvider();
            _dataProtector = services.GetDataProtector("SamplePurpose");

            _protectedData = _dataProtector.Protect(AntiforgeryTokenSample);
        }

        [Benchmark]
        public string Protect()
        {
            var encrypted = _dataProtector.Protect(AntiforgeryTokenSample);
            return encrypted;
        }

        [Benchmark]
        public string Unprotect()
        {
            var encrypted = _dataProtector.Unprotect(_protectedData);
            return encrypted;
        }
    }
}
