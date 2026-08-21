using System.ComponentModel.DataAnnotations;
using CoreClaims.Infrastructure.BusinessRules;
using CoreClaims.Infrastructure.Domain.Entities;
using CoreClaims.Infrastructure.Domain.Enums;
using CoreClaims.Infrastructure.Repository;
using Microsoft.Extensions.Options;
using Moq;

namespace CoreClaims.Tests;

public class CoreBusinessRuleTests
{
    private readonly Mock<IAdjudicatorRepository> adjudicators = new();
    private readonly Mock<IMemberRepository> members = new();

    private CoreBusinessRule Create(BusinessRuleOptions options = null)
    {
        options ??= new BusinessRuleOptions
        {
            DemoAdjudicatorId = "demo",
            DemoManagerAdjudicatorId = "manager"
        };
        return new CoreBusinessRule(adjudicators.Object, members.Object, Options.Create(options));
    }

    private static ClaimDetail Claim(decimal total = 100, string member = "member") => new()
    {
        ClaimId = "claim-1",
        MemberId = member,
        FilingDate = new DateTime(2024, 1, 15),
        TotalAmount = total,
        LineItems = Array.Empty<LineItem>()
    };

    [Fact]
    public async Task AssignClaim_HandlesPendingCoverageApprovalAndAssignment()
    {
        var rule = Create();
        members.Setup(x => x.GetMemberCoverage("member")).ReturnsAsync(new[]
        {
            new Coverage { StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31) }
        });
        adjudicators.Setup(x => x.GetAdjudicator("demo")).ReturnsAsync(new Adjudicator
        {
            AdjudicatorId = "demo", Role = AdjudicatorRole.Adjudicator
        });
        adjudicators.Setup(x => x.GetAdjudicator("manager")).ReturnsAsync(new Adjudicator
        {
            AdjudicatorId = "demo", Role = AdjudicatorRole.Adjudicator
        });

        Assert.Equal(ClaimStatus.Pending, (await rule.AssignClaim(Claim(member: ""))).ClaimStatus);

        members.Setup(x => x.GetMemberCoverage("member")).ReturnsAsync(Array.Empty<Coverage>());
        var denied = await rule.AssignClaim(Claim(300));
        Assert.Equal(ClaimStatus.Denied, denied.ClaimStatus);
        Assert.Contains("[Automatic] Rejected", denied.Comment);

        members.Setup(x => x.GetMemberCoverage("member")).ReturnsAsync(new[]
        {
            new Coverage { StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31) }
        });
        var approved = await rule.AssignClaim(Claim(199));
        Assert.Equal(ClaimStatus.Approved, approved.ClaimStatus);

        var assigned = await rule.AssignClaim(Claim(300));
        Assert.Equal("demo", assigned.AdjudicatorId);
        Assert.Equal(ClaimStatus.Assigned, assigned.ClaimStatus);
    }

    [Fact]
    public async Task AssignClaim_FallsBackForUnknownDemoAndPreservesExistingAssignment()
    {
        var options = new BusinessRuleOptions { DemoMode = true, DemoAdjudicatorId = "unknown" };
        var fallback = new Adjudicator { AdjudicatorId = "random", Role = AdjudicatorRole.Adjudicator };
        adjudicators.Setup(x => x.GetAdjudicator("unknown")).ReturnsAsync((Adjudicator)null);
        adjudicators.Setup(x => x.GetRandomAdjudicator("Adjudicator", 0, It.IsAny<int>())).ReturnsAsync(fallback);
        members.Setup(x => x.GetMemberCoverage("member")).ReturnsAsync(new[]
        {
            new Coverage { StartDate = DateTime.MinValue, EndDate = DateTime.MaxValue }
        });

        var result = await Create(options).AssignClaim(Claim(300));
        Assert.Equal("random", result.AdjudicatorId);
        adjudicators.Verify(x => x.GetRandomAdjudicator("Adjudicator", 0, It.IsAny<int>()), Times.Once);

        var existing = Claim(300);
        existing.AdjudicatorId = "already-assigned";
        var unchanged = await Create(options).AssignClaim(existing);
        Assert.Equal("already-assigned", unchanged.AdjudicatorId);
        adjudicators.Verify(x => x.GetAdjudicator("unknown"), Times.Once);
    }

    [Fact]
    public async Task AdjudicateClaim_ApprovesManagerAndSmallChanges()
    {
        adjudicators.Setup(x => x.GetAdjudicator("manager")).ReturnsAsync(new Adjudicator
        {
            AdjudicatorId = "manager", Role = AdjudicatorRole.Manager
        });
        var managerResult = await Create().AdjudicateClaim(new ClaimDetail { AdjudicatorId = "manager", TotalAmount = 100 });
        Assert.Equal(ClaimStatus.Approved, managerResult.Item1.ClaimStatus);
        Assert.False(managerResult.Item2);

        adjudicators.Setup(x => x.GetAdjudicator("staff")).ReturnsAsync(new Adjudicator
        {
            AdjudicatorId = "staff", Role = AdjudicatorRole.Adjudicator
        });
        var small = new ClaimDetail { AdjudicatorId = "staff", LastAmount = 100, TotalAmount = 599 };
        var smallResult = await Create().AdjudicateClaim(small);
        Assert.Equal(ClaimStatus.Approved, smallResult.Item1.ClaimStatus);
        Assert.False(smallResult.Item2);
    }

    [Fact]
    public async Task AdjudicateClaim_ReassignsWhenManagerApprovalIsRequired()
    {
        adjudicators.Setup(x => x.GetAdjudicator("staff")).ReturnsAsync(new Adjudicator
        {
            AdjudicatorId = "staff", Role = AdjudicatorRole.Adjudicator
        });
        adjudicators.Setup(x => x.GetAdjudicator("manager")).ReturnsAsync(new Adjudicator
        {
            AdjudicatorId = "manager", Role = AdjudicatorRole.Manager
        });

        var claim = new ClaimDetail { AdjudicatorId = "staff", LastAmount = 100, TotalAmount = 700 };
        var result = await Create().AdjudicateClaim(claim);

        Assert.Equal(ClaimStatus.ApprovalRequired, result.Item1.ClaimStatus);
        Assert.Equal("staff", result.Item1.PreviousAdjudicatorId);
        Assert.Equal("manager", result.Item1.AdjudicatorId);
        Assert.True(result.Item2);
    }

    [Fact]
    public async Task AdjudicateClaim_ThrowsWhenNoManagerExists()
    {
        adjudicators.Setup(x => x.GetAdjudicator("staff")).ReturnsAsync(new Adjudicator
        {
            AdjudicatorId = "staff", Role = AdjudicatorRole.Adjudicator
        });
        adjudicators.Setup(x => x.GetAdjudicator("manager")).ReturnsAsync((Adjudicator)null);
        adjudicators.Setup(x => x.GetRandomAdjudicator("Manager", 0, It.IsAny<int>())).ReturnsAsync((Adjudicator)null);

        await Assert.ThrowsAsync<NullReferenceException>(() =>
            Create().AdjudicateClaim(new ClaimDetail { AdjudicatorId = "staff", LastAmount = 1, TotalAmount = 1000 }));
    }

    [Fact]
    public void BusinessRuleOptions_HaveDefaultsAndValidateRanges()
    {
        var options = new BusinessRuleOptions();
        Assert.Equal(200, options.AutoApproveThreshold);
        Assert.Equal(500, options.RequireManagerApproval);
        Assert.True(options.DemoMode);

        options.AutoApproveThreshold = -1;
        Assert.False(Validator.TryValidateObject(options, new ValidationContext(options), new List<ValidationResult>(), true));
        options.AutoApproveThreshold = 200;
        options.RequireManagerApproval = 20001;
        Assert.False(Validator.TryValidateObject(options, new ValidationContext(options), new List<ValidationResult>(), true));
    }
}
