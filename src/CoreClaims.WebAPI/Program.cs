
using System.Text.Json.Serialization;
using CoreClaims.Infrastructure;
using CoreClaims.Infrastructure.BusinessRules;
using CoreClaims.Infrastructure.Events;
using CoreClaims.Infrastructure.Identity;
using CoreClaims.Infrastructure.Repository;
using CoreClaims.SemanticKernel;
using CoreClaims.WebAPI.Components;
using CoreClaims.WebAPI.Endpoints.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.DependencyInjection;

namespace CoreClaims.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var allowAllCorsOrigins = "AllowAllOrigins";

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            //builder.Logging.AddApplicationInsights(
            //    configureTelemetryConfiguration: (config) =>
            //        config.ConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING"),
            //    configureApplicationInsightsLoggerOptions: (options) => { }
            //);

            // Add services to the container.
            //builder.Services.AddAuthorization();

            builder.Services.Configure<BusinessRuleOptions>(builder.Configuration.GetSection(nameof(BusinessRuleOptions)));
            builder.Services.AddCors(policyBuilder =>
            {
                policyBuilder.AddPolicy(allowAllCorsOrigins,
                    policy =>
                    {
                        policy.AllowAnyOrigin();
                        policy.AllowAnyHeader();
                        policy.AllowAnyMethod();
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

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

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

            app.UseCors(allowAllCorsOrigins);

            //app.UseAuthorization();

            app.Run();
        }
    }
}
