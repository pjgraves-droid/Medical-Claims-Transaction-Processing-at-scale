using CoreClaims.Infrastructure;
using CoreClaims.Infrastructure.Domain.Entities;
using CoreClaims.Infrastructure.Repository;
using Microsoft.Azure.Cosmos;
using Moq;

namespace CoreClaims.Tests;

public class MemberRepositoryTests
{
    private static (MemberRepository Repository, Mock<Container> Container) CreateRepository()
    {
        var client = new Mock<CosmosClient>();
        var database = new Mock<Database>();
        var container = new Mock<Container>();
        client.Setup(x => x.GetDatabase(Constants.Connections.CosmosDbName)).Returns(database.Object);
        database.Setup(x => x.GetContainer("Member")).Returns(container.Object);
        return (new MemberRepository(client.Object), container);
    }

    [Fact]
    public async Task ListMemberClaims_BuildsFiltersDatesPagingAndSort()
    {
        var (repository, container) = CreateRepository();
        var queries = new List<QueryDefinition>();
        container.Setup(x => x.GetItemQueryIterator<int>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string, QueryRequestOptions>((query, _, _) => queries.Add(query))
            .Returns(TestCosmosHelpers.Iterator(new[] { 3 }));
        container.Setup(x => x.GetItemQueryIterator<ClaimHeader>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string, QueryRequestOptions>((query, _, _) => queries.Add(query))
            .Returns(TestCosmosHelpers.Iterator(new[] { new ClaimHeader { ClaimId = "claim" } }));

        var result = await repository.ListMemberClaims("member", 20, 10,
            new DateTime(2024, 1, 1), new DateTime(2024, 1, 31), false, null, "desc");
        var text = string.Join("\n", queries.Select(x => x.QueryText));

        Assert.Contains("m.claimStatus != 'Denied'", text);
        Assert.Contains("m.filingDate >= @startDate", text);
        Assert.Contains("m.filingDate <= @endDate", text);
        Assert.Contains("ORDER BY m._ts desc", text);
        Assert.Contains("@offset", text);
        Assert.Contains("@limit", text);
        Assert.Equal(3, result.Total);
        Assert.Equal(20, ((CoreClaims.Infrastructure.Models.PageResult<ClaimHeader>)result).Offset);
        Assert.Equal(10, result.Size);
        Assert.Single(result.Items);
        Assert.Contains(queries, query => query.GetQueryParameters().Any(p => p.Name == "@startDate"));
        Assert.Contains(queries, query => query.GetQueryParameters().Any(p => p.Name == "@endDate"));
    }

    [Fact]
    public async Task ListMemberClaims_IncludeDeniedOmitsDeniedFilter()
    {
        var (repository, container) = CreateRepository();
        var queries = new List<QueryDefinition>();
        container.Setup(x => x.GetItemQueryIterator<int>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string, QueryRequestOptions>((query, _, _) => queries.Add(query))
            .Returns(TestCosmosHelpers.Iterator(new[] { 1 }));
        container.Setup(x => x.GetItemQueryIterator<ClaimHeader>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string, QueryRequestOptions>((query, _, _) => queries.Add(query))
            .Returns(TestCosmosHelpers.Iterator(Array.Empty<ClaimHeader>()));

        await repository.ListMemberClaims("member", includeDenied: true);
        Assert.DoesNotContain(queries, q => q.QueryText.Contains("claimStatus != 'Denied'"));
    }

    [Fact]
    public async Task CoverageAndMembersQueriesUseExpectedShapes()
    {
        var (repository, container) = CreateRepository();
        var queries = new List<QueryDefinition>();
        container.Setup(x => x.GetItemQueryIterator<int>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string, QueryRequestOptions>((query, _, _) => queries.Add(query))
            .Returns(TestCosmosHelpers.Iterator(new[] { 2 }));
        container.Setup(x => x.GetItemQueryIterator<Coverage>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string, QueryRequestOptions>((query, _, _) => queries.Add(query))
            .Returns(TestCosmosHelpers.Iterator(new[] { new Coverage { MemberId = "member" } }));
        container.Setup(x => x.GetItemQueryIterator<Member>(It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition, string, QueryRequestOptions>((query, _, _) => queries.Add(query))
            .Returns(TestCosmosHelpers.Iterator(new[] { new Member { MemberId = "member" } }));

        var coverage = await repository.GetMemberCoverage("member");
        var members = await repository.ListMembers(5, 10, null, "desc");

        Assert.Single(coverage);
        Assert.Equal(2, members.Total);
        Assert.Contains(queries, q => q.QueryText.Contains("m.type = 'Coverage'"));
        Assert.Contains(queries, q => q.QueryText.Contains("m.type = 'Member'"));
        Assert.Contains(queries, q => q.QueryText.Contains("ORDER BY m._ts desc"));
        Assert.Contains(queries, q => q.GetQueryParameters().Any(p => p.Name == "@offset"));
    }

    [Fact]
    public async Task IncrementMemberTotalsReturnsPatchedResource()
    {
        var (repository, container) = CreateRepository();
        var member = new Member { MemberId = "member", ApprovedCount = 4, ApprovedTotal = 50 };
        var response = new Mock<ItemResponse<Member>>();
        response.SetupGet(x => x.Resource).Returns(member);
        PatchOperation[] operations = null;
        container.Setup(x => x.PatchItemAsync<Member>("member", It.IsAny<PartitionKey>(),
                It.IsAny<IReadOnlyList<PatchOperation>>(), It.IsAny<PatchItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, PartitionKey, IReadOnlyList<PatchOperation>, PatchItemRequestOptions, CancellationToken>((_, _, patch, _, _) => operations = patch.ToArray())
            .ReturnsAsync(response.Object);

        var result = await repository.IncrementMemberTotals("member", 2, 12.5m);
        Assert.Same(member, result);
        Assert.NotNull(operations);
        Assert.Equal(4, operations.Length);
        Assert.Contains(operations, x => x.Path == "/approvedCount");
        Assert.Contains(operations, x => x.Path == "/approvedTotal");
        Assert.Contains(operations, x => x.Path == "/modifiedBy");
        Assert.Contains(operations, x => x.Path == "/modifiedOn");
    }
}
