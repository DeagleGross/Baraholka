using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class AntiforgeryBenchmarks
    {
        private IAntiforgery _antiforgery;

        AntiforgeryTokenSet _tokenSet;

        HttpContext _incomingRequestCtx;

        [GlobalSetup]
        public void Setup()
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>()
                .AddAntiforgery();

            serviceCollection.Configure<AntiforgeryOptions>(options => {
                options.HeaderName = "XSRF-TOKEN";
            });

            var services = serviceCollection.BuildServiceProvider();
            _antiforgery = services.GetRequiredService<IAntiforgery>();

            var cookieName = services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.Cookie.Name;

            _tokenSet = _antiforgery.GetAndStoreTokens(new DefaultHttpContext());
            _incomingRequestCtx = new DefaultHttpContext();
            _incomingRequestCtx.Request.Headers["XSRF-TOKEN"] = _tokenSet.RequestToken;
            _incomingRequestCtx.Request.Headers["Cookie"] = $"{cookieName}={_tokenSet.CookieToken}";
        }

        [Benchmark]
        public object Generate()
        {
            var data = _antiforgery.GetAndStoreTokens(new DefaultHttpContext());
            return data;
        }

        [Benchmark]
        public async Task Validate()
        {
            await _antiforgery.ValidateRequestAsync(_incomingRequestCtx);
        }
    }
}
