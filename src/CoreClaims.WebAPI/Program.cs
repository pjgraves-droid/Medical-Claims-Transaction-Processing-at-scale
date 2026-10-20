
using System.Text.Json.Serialization;
using CoreClaims.Infrastructure;
using CoreClaims.Infrastructure.BusinessRules;
using CoreClaims.Infrastructure.Events;
using CoreClaims.Infrastructure.Identity;
using CoreClaims.Infrastructure.Repository;
using CoreClaims.SemanticKernel;
using CoreClaims.WebAPI.Components;
using CoreClaims.WebAPI.Endpoints.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Constants = CoreClaims.Infrastructure.Constants;

namespace CoreClaims.WebAPI
{
    public class Program
    {
        public const string FrontendCorsPolicy = "Frontend";
        public const string HealthCheckPath = "/health";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            //builder.Logging.AddApplicationInsights(
            //    configureTelemetryConfiguration: (config) =>
            //        config.ConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING"),
            //    configureApplicationInsightsLoggerOptions: (options) => { }
            //);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection(Constants.Identity.AzureAdSection));

            // Every endpoint requires an authenticated caller unless it explicitly opts out (health checks).
            builder.Services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

            builder.Services.AddHealthChecks();

            builder.Services.Configure<BusinessRuleOptions>(builder.Configuration.GetSection(nameof(BusinessRuleOptions)));

            var allowedOrigins = (builder.Configuration.GetSection(Constants.Cors.AllowedOrigins).Get<string[]>() ?? [])
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim().TrimEnd('/'))
                .ToArray();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(FrontendCorsPolicy, policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var azureCredential = AzureCredentialFactory.Create(builder.Configuration[Constants.Identity.ClientId]);

            builder.Services.AddSingleton(s =>
            {
                var endpoint = builder.Configuration[Constants.Connections.CosmosDbEndpoint];
                return new CosmosClientBuilder(endpoint, azureCredential)
                    .Build();
            });
            builder.Services.AddSingleton<IEventHubService, EventHubService>(s => new EventHubService(
                builder.Configuration[Constants.Connections.EventHubNamespace], builder.Configuration[Constants.Identity.ClientId]));

            builder.Services.AddSingleton<IClaimRepository, ClaimRepository>();
            builder.Services.AddSingleton<IAdjudicatorRepository, AdjudicatorRepository>();
            builder.Services.AddSingleton<IMemberRepository, MemberRepository>();
            builder.Services.AddSingleton<IClaimProcedureRepository, ClaimProcedureRepository>();
            builder.Services.AddSingleton<IPayerRepository, PayerRepository>();
            builder.Services.AddSingleton<IProviderRepository, ProviderRepository>();

            builder.Services.AddSingleton<ICoreBusinessRule, CoreBusinessRule>();
            builder.Services.AddRulesEngine(builder.Configuration, azureCredential);

            // Add Endpoint classes.
            builder.Services.AddScoped<EndpointsBase, AdjudicatorEndpoints>();
            builder.Services.AddScoped<EndpointsBase, BusinessRuleEndpoints>();
            builder.Services.AddScoped<EndpointsBase, ClaimEndpoints>();
            builder.Services.AddScoped<EndpointsBase, ClaimProcedureEndpoints>();
            builder.Services.AddScoped<EndpointsBase, MemberEndpoints>();
            builder.Services.AddScoped<EndpointsBase, PayerEndpoints>();
            builder.Services.AddScoped<EndpointsBase, ProviderEndpoints>();

            // TODO: Implement serialization resolver and rules
            builder.Services.ConfigureHttpJsonOptions(options => {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Services.AddOpenApi();

            var app = builder.Build();

            app.UseHttpsRedirection();

            app.UseCors(FrontendCorsPolicy);
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks(HealthCheckPath).AllowAnonymous();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi().AllowAnonymous();
            }

            // Map the REST endpoints:
            using (var scope = app.Services.CreateScope())
            {
                // Build collection of all EndpointsBase classes
                var services = scope.ServiceProvider.GetServices<EndpointsBase>();
                // Loop through each EndpointsBase class
                foreach (var item in services)
                {
                    // Invoke the AddRoutes() method to add the routes
                    item.AddRoutes(app);
                }
            }

            app.Run();
        }
    }
}
