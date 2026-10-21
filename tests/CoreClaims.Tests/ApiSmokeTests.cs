using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CoreClaims.Infrastructure.Domain.Entities;
using CoreClaims.Infrastructure.Models;
using CoreClaims.Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreClaims.Tests;

public class ApiFactory : WebApplicationFactory<CoreClaims.WebAPI.Program>
{
    public const string FrontendOrigin = "http://localhost:3000";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("CosmosDBConnection:accountEndpoint", "https://localhost:8081/");
        builder.UseSetting("CoreClaimsCosmosDB:accountEndpoint", "https://localhost:8081/");
        builder.UseSetting("CoreClaimsEventHub:fullyQualifiedNamespace", "test.servicebus.windows.net");
        builder.UseSetting("RulesEngine:OpenAIEndpoint", "https://test.openai.azure.com/");
        builder.UseSetting("RulesEngine:OpenAICompletionsDeployment", "test");
        builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
        builder.UseSetting("AzureAd:TenantId", "00000000-0000-0000-0000-000000000001");
        builder.UseSetting("AzureAd:ClientId", "00000000-0000-0000-0000-000000000002");
        builder.UseSetting("Cors:AllowedOrigins:0", FrontendOrigin);
    }
}

public class AuthenticatedApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.RemoveAll<IPayerRepository>();
            services.AddSingleton<IPayerRepository, FakePayerRepository>();
        });
    }

    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], SchemeName);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }

    private sealed class FakePayerRepository : IPayerRepository
    {
        public Task<IPageResult<Payer>> ListPayers(int offset = 0, int limit = 10, string sortColumn = "id", string sortDirection = "asc")
            => Task.FromResult<IPageResult<Payer>>(new PageResult<Payer>(1, offset, limit, [new Payer { PayerId = "p1", Name = "Contoso Health" }]));

        public Task<Payer> GetPayer(string payerId) => Task.FromResult(new Payer { PayerId = payerId });
    }
}

public class ApiSmokeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_IsAnonymous()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/payers")]
    [InlineData("/api/providers")]
    [InlineData("/api/claim/abc")]
    public async Task ApiEndpoints_RequireAuthentication(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(ApiFactory.FrontendOrigin, true)]
    [InlineData("https://evil.example.com", false)]
    public async Task Cors_OnlyAllowsConfiguredFrontendOrigin(string origin, bool allowed)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/payers");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}

public class AuthenticatedApiSmokeTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    [Fact]
    public async Task ListPayers_ReturnsRepositoryData_WhenAuthenticated()
    {
        var response = await factory.CreateClient().GetAsync("/api/payers");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Contoso Health", body);
    }
}
