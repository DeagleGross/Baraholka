using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
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
                .AddAntiforgery();

            serviceCollection.Configure<AntiforgeryOptions>(options => {
                options.HeaderName = "XSRF-TOKEN";
            });

            var services = serviceCollection.BuildServiceProvider();
            _antiforgery = services.GetRequiredService<IAntiforgery>();

            var cookieName = services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.Cookie.Name;

            _incomingRequestCtx = PrepareRequest(_antiforgery, cookieName, withIdentity: false);
            _incomingRequestWithUserCtx = PrepareRequest(_antiforgery, cookieName, withIdentity: true);
        }

        [Benchmark]
        public object Generate()
        {
            var data = _antiforgery.GetAndStoreTokens(new DefaultHttpContext());
            return data;
        }

        [Benchmark]
        public Task ValidateWithUser() => _antiforgery.ValidateRequestAsync(_incomingRequestWithUserCtx);
        [Benchmark]
        public Task Validate() => _antiforgery.ValidateRequestAsync(_incomingRequestCtx);

        private static HttpContext PrepareRequest(IAntiforgery antiforgery, string cookieName, bool withIdentity = false)
        {
            // Simulate initial request to get tokens and capture Set-Cookie header
            var ctx = new DefaultHttpContext();
            if (withIdentity)
            {
                ctx.User = new ClaimsPrincipal(GetAuthenticatedIdentity("the-user"));
            }

            var tokens = antiforgery.GetAndStoreTokens(ctx);

            // Extract the Set-Cookie header from the response (written by GetAndStoreTokens)
            var setCookieHeader = ctx.Response.Headers["Set-Cookie"];
            var cookieValue = setCookieHeader
                .FirstOrDefault(h => h.StartsWith(cookieName + "="))?
                .Split(';')[0]  // e.g. XSRF-TOKEN=abc123...
                .Substring(cookieName.Length + 1); // Just the value

            if (cookieValue == null)
                throw new InvalidOperationException("Failed to extract antiforgery cookie.");

            // Set headers on your test context
            var context = new DefaultHttpContext();
            context.Request.Headers["XSRF-TOKEN"] = tokens.RequestToken;
            context.Request.Headers["Cookie"] = $"{cookieName}={cookieValue}";
            if (withIdentity)
            {
                context.User = ctx.User;
            }

            return context;

            ClaimsIdentity GetAuthenticatedIdentity(string identityUsername)
            {
                var claim = new Claim(ClaimsIdentity.DefaultNameClaimType, identityUsername);
                return new ClaimsIdentity(new[] { claim }, "Some-Authentication");
            }
        }
    }
}
