using CoreClaims.Infrastructure;
using CoreClaims.Infrastructure.Domain.Entities;
using CoreClaims.Infrastructure.Repository;
using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;

namespace CoreClaims.Tests;

public sealed class CosmosEmulatorFixture : IAsyncLifetime
{
    private readonly CosmosDbContainer _container = new CosmosDbBuilder()
        .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
        .Build();

    public CosmosClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        Client = new CosmosClient(_container.GetConnectionString(), new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            HttpClientFactory = () => _container.HttpClient,
        });

        var database = (await Client.CreateDatabaseIfNotExistsAsync(Constants.Connections.CosmosDbName)).Database;
        var payers = (await database.CreateContainerIfNotExistsAsync("Payer", "/payerId")).Container;

        foreach (var name in new[] { "Contoso Health", "Fabrikam Insurance", "Northwind Mutual" })
        {
            var payer = new Payer { PayerId = name.Replace(' ', '-').ToLowerInvariant(), Name = name };
            await payers.UpsertItemAsync(payer, new PartitionKey(payer.PayerId));
        }
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await _container.DisposeAsync();
    }
}

[Trait("Category", "Integration")]
public class CosmosDbRepositoryTests(CosmosEmulatorFixture cosmos) : IClassFixture<CosmosEmulatorFixture>
{
    [Fact]
    public async Task ReadItem_ReturnsEntity_WhenItExists()
    {
        var payer = await new PayerRepository(cosmos.Client).GetPayer("contoso-health");

        Assert.NotNull(payer);
        Assert.Equal("Contoso Health", payer.Name);
    }

    [Fact]
    public async Task ReadItem_ReturnsNull_WhenItemIsMissing()
    {
        var payer = await new PayerRepository(cosmos.Client).GetPayer("does-not-exist");

        Assert.Null(payer);
    }

    [Fact]
    public async Task Query_PagesAndSortsAcrossPartitions()
    {
        var page = await new PayerRepository(cosmos.Client).ListPayers(offset: 1, limit: 1, sortColumn: "name", sortDirection: "desc");

        Assert.Equal(3, page.Total);
        var payer = Assert.Single(page.Items);
        Assert.Equal("Fabrikam Insurance", payer.Name);
    }
}
