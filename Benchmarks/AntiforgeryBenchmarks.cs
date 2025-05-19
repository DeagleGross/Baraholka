using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Antiforgery.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class AntiforgeryBenchmarks
    {
        private IAntiforgery _antiforgery;

        HttpContext _incomingRequestCtx;
        HttpContext _incomingRequestWithUserCtx;

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

            _incomingRequestCtx = new DefaultHttpContext();
            _incomingRequestWithUserCtx = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(GetAuthenticatedIdentity("the-user"))
            };

            _incomingRequestCtx.Request.Headers["XSRF-TOKEN"] = "CfDJ8A0IIE8u6t5FsaKqaPkasDmubNpMx80apZHWZrsPmdpBXsWnh3Ildqnk_3rZtiEsqy9tjqXzjpGrCos-6fnIUCkxkAgwLHey4C1XM--ueeFgKZ8AB-Ouh3zWy6xVtNZRd9hnVmuNiPKNdWkoDkKwlLE";
            _incomingRequestCtx.Request.Headers["Cookie"] = $"{cookieName}=CfDJ8A0IIE8u6t5FsaKqaPkasDn6SJIKfTcyhi-cLzwa0-HgMchyjVeaw4NwAnQc0xcRx3lidshjZz5yj8a1Njc3X_n-4EOM6AzTGxwDPEXlLvtjNisjJj1Me72lbf_OPuy7JgpVTKr-E8UbJgOmlxaV4fs";

            _incomingRequestWithUserCtx.Request.Headers["XSRF-TOKEN"] = "CfDJ8A0IIE8u6t5FsaKqaPkasDkMhe6t3h63Kfj4EbBvtt0hQQ_PRmJ_wGfd__jcQJmoivqSo4dZuO-Pw8fiPCRXKEJ2RWZht0pZnR3jdTZv2hxoCm88aTx1t9yEpu-sAKhRSc_uCoqvktu25HheU1TSKqHJM4vAOMLWA5dvRnHK3isZk73eNLfmPm5tICKv_NXiDw";
            _incomingRequestWithUserCtx.Request.Headers["Cookie"] = $"{cookieName}=CfDJ8A0IIE8u6t5FsaKqaPkasDloVgzUs-caOwK3jtE5xvl112d-AQxUB5tf1DC_NKNRJxxwQ_Iffj2Mhc9RcOeSGt5vkfz3V2NhR2w4CfC4NqXH1ppMCgviD5pDFaj6lcr27jQOwxkwOjmvj6LzX2i8ulo";
        }

        [Benchmark]
        public object Generate()
        {
            var data = _antiforgery.GetAndStoreTokens(new DefaultHttpContext());
            return data;
        }

        [Benchmark]
        public async Task ValidateWithUser()
        {
            await _antiforgery.ValidateRequestAsync(_incomingRequestWithUserCtx);
        }

        [Benchmark]
        public async Task Validate()
        {
            await _antiforgery.ValidateRequestAsync(_incomingRequestCtx);
        }

        private static ClaimsIdentity GetAuthenticatedIdentity(string identityUsername)
        {
            var claim = new Claim(ClaimsIdentity.DefaultNameClaimType, identityUsername);
            return new ClaimsIdentity(new[] { claim }, "Some-Authentication");
        }
    }
}
