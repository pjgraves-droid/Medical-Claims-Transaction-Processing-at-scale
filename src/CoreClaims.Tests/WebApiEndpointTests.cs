using CoreClaims.Infrastructure.BusinessRules;
using CoreClaims.Infrastructure.Domain.Entities;
using CoreClaims.Infrastructure.Domain.Enums;
using CoreClaims.Infrastructure.Events;
using CoreClaims.Infrastructure.Models;
using CoreClaims.Infrastructure.Repository;
using CoreClaims.SemanticKernel;
using CoreClaims.WebAPI.Endpoints.Http;
using CoreClaims.WebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CoreClaims.Tests;

public class WebApiEndpointTests
{
    private sealed class TestMemberEndpoints : MemberEndpoints
    {
        public TestMemberEndpoints(IMemberRepository repository, ILogger<MemberEndpoints> logger)
            : base(repository, Options.Create(new BusinessRuleOptions()), logger) { }
        public Task<IResult> Claims(string id, HttpRequest request) => ListMemberClaims(id, request);
        public Task<IResult> Member(string id) => GetMember(id);
        public Task<IResult> Members(HttpRequest request) => ListMembers(request);
        public Task<IResult> Coverage(string id) => GetMemberCoverage(id);
    }

    private sealed class TestClaimEndpoints : ClaimEndpoints
    {
        public TestClaimEndpoints(IClaimRepository claims, IRulesEngine rules, IEventHubService eventHub,
            ILogger<ClaimEndpoints> logger)
            : base(claims, Options.Create(new BusinessRuleOptions()), rules, eventHub, logger) { }
        public Task<IResult> Get(string id) => GetClaimAdjudication(id);
        public Task<IResult> Update(string id, UpdateClaimModel model) => UpdateClaimAdjudication(id, model);
    }

    private sealed class TestBusinessRuleEndpoints : BusinessRuleEndpoints
    {
        public TestBusinessRuleEndpoints(IOptions<BusinessRuleOptions> options, ILogger<BusinessRuleEndpoints> logger)
            : base(options, logger) { }
        public IResult Rules() => Get();
    }

    private static DefaultHttpContext Request(string query)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        return context;
    }

    [Fact]
    public async Task MemberEndpoints_ReturnExpectedResultsAndForwardQueryParameters()
    {
        var repository = new Mock<IMemberRepository>();
        var logger = new Mock<ILogger<MemberEndpoints>>();
        var page = new Mock<IPageResult<ClaimHeader>>().Object;
        repository.Setup(x => x.ListMemberClaims("member", 20, 10,
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), true, "filingDate", "desc"))
            .ReturnsAsync(page);
        repository.Setup(x => x.Get("member")).ReturnsAsync((Member)null);
        repository.Setup(x => x.GetMemberCoverage("member")).ReturnsAsync(new[]
        {
            new Coverage { CoverageId = "coverage" }
        });
        repository.Setup(x => x.ListMembers(0, 10, null, null)).ReturnsAsync(new Mock<IPageResult<Member>>().Object);

        var endpoints = new TestMemberEndpoints(repository.Object, logger.Object);
        var claims = await endpoints.Claims("member",
            Request("?offset=20&limit=10&startDate=2024-01-01&endDate=2024-01-31&sortColumn=filingDate&sortDirection=desc").Request);
        var notFound = await endpoints.Member("member");
        var coverage = await endpoints.Coverage("member");
        var members = await endpoints.Members(Request("?limit=10").Request);

        Assert.IsType<Ok<IPageResult<ClaimHeader>>>(claims);
        Assert.IsType<NotFound>(notFound);
        Assert.IsType<Ok<Coverage>>(coverage);
        Assert.IsType<Ok<IPageResult<Member>>>(members);
        repository.Verify(x => x.ListMemberClaims("member", 20, 10,
            new DateTime(2024, 1, 1), new DateTime(2024, 1, 31), true, "filingDate", "desc"), Times.Once);
    }

    [Fact]
    public void BusinessRuleEndpoints_ReturnConfiguredOptions()
    {
        var options = Options.Create(new BusinessRuleOptions { AutoApproveThreshold = 125 });
        var endpoints = new TestBusinessRuleEndpoints(options, new Mock<ILogger<BusinessRuleEndpoints>>().Object);
        var result = Assert.IsType<Ok<BusinessRuleOptions>>(endpoints.Rules());
        Assert.Equal(125, result.Value.AutoApproveThreshold);
    }

    [Fact]
    public async Task ClaimEndpoints_HandleNotFoundAndInvalidStatus()
    {
        var claims = new Mock<IClaimRepository>();
        var rules = new Mock<IRulesEngine>();
        var eventHub = new Mock<IEventHubService>();
        var logger = new Mock<ILogger<ClaimEndpoints>>();
        claims.Setup(x => x.GetClaim("missing", null)).ReturnsAsync((ClaimDetail)null);
        claims.Setup(x => x.GetClaim("claim", null)).ReturnsAsync(new ClaimDetail
        {
            ClaimId = "claim", ClaimStatus = ClaimStatus.Initial, LineItems = Array.Empty<LineItem>()
        });
        var endpoints = new TestClaimEndpoints(claims.Object, rules.Object, eventHub.Object, logger.Object);

        Assert.IsType<NotFound>(await endpoints.Get("missing"));
        var invalid = await endpoints.Update("claim", new UpdateClaimModel { ClaimStatus = ClaimStatus.Approved });
        var badRequest = Assert.IsType<BadRequest<string>>(invalid);
        Assert.Contains("Only claims", badRequest.Value);
    }
}
