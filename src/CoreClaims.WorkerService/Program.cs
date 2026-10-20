using CoreClaims.Infrastructure;
using CoreClaims.Infrastructure.BusinessRules;
using CoreClaims.Infrastructure.Events;
using CoreClaims.Infrastructure.Repository;
using CoreClaims.WorkerService;
using Microsoft.Azure.Cosmos.Fluent;
using CoreClaims.Infrastructure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

// Emit Azure SDK (Cosmos DB, Event Hubs) spans as OpenTelemetry activities.
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
var openTelemetry = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("CoreClaims.WorkerService"))
    .WithTracing(tracing => tracing.AddSource("Azure.*"));
var aiConnectionString = builder.Configuration[Constants.Telemetry.ConnectionString]
    ?? builder.Configuration[Constants.Telemetry.ConnectionStringEnvironmentVariable];
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    openTelemetry.UseAzureMonitorExporter(options => options.ConnectionString = aiConnectionString);
}

builder.Services.Configure<BusinessRuleOptions>(builder.Configuration.GetSection(nameof(BusinessRuleOptions)));

builder.Services.AddSingleton(s =>
{
    var endpoint = builder.Configuration[Constants.Connections.CosmosDbEndpoint];
    var credential = AzureCredentialFactory.Create(builder.Configuration[Constants.Identity.ClientId]);

    return new CosmosClientBuilder(endpoint, credential)
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

builder.Services.AddSingleton<ICosmosDbChangeFeedService, CosmosDbChangeFeedService>();

builder.Services.AddHostedService<ChangeFeedWorker>();
builder.Services.AddHostedService<EventHubWorkerIncomingClaims>();
builder.Services.AddHostedService<EventHubWorkerAdjudicatorChanged>();

var host = builder.Build();

host.Run();
