using CoreClaims.Infrastructure;
using CoreClaims.Infrastructure.Domain.Entities;
using CoreClaims.Infrastructure.Domain.Enums;
using CoreClaims.Infrastructure.Events;
using CoreClaims.Infrastructure.Helpers;
using CoreClaims.Infrastructure.Repository;
using CoreClaims.WorkerService;
using Microsoft.Extensions.Logging;
using Moq;

namespace CoreClaims.Tests;

public class WorkerTests
{
    private sealed class IncomingWorker : EventHubWorkerIncomingClaims
    {
        public IncomingWorker(ILogger<EventHubWorkerIncomingClaims> logger, IEventHubService hub,
            IClaimRepository claims, IProviderRepository providers, IPayerRepository payers)
            : base(logger, hub, claims, providers, payers) { }
        public Task Process(string message) => ProcessIncomingClaim(message);
    }

    private sealed class ChangedWorker : EventHubWorkerAdjudicatorChanged
    {
        public ChangedWorker(ILogger<EventHubWorkerAdjudicatorChanged> logger, IEventHubService hub,
            IAdjudicatorRepository adjudicators) : base(logger, hub, adjudicators) { }
        public Task Process(string message) => ProcessAdjudictorChanged(message);
    }

    private sealed class ChangeFeed : ChangeFeedWorker
    {
        public ChangeFeed(ILogger<ChangeFeedWorker> logger, ICosmosDbChangeFeedService service)
            : base(logger, service) { }
        public Task Run() => ExecuteAsync(CancellationToken.None);
    }

    private static string Message(ClaimDetail detail) => JsonSerializationHelper.SerializeItem(detail);

    private static ClaimDetail Detail(string id, ClaimStatus status = ClaimStatus.Initial) => new()
    {
        ClaimId = id,
        ClaimStatus = status,
        FilingDate = new DateTime(2024, 1, 1),
        LineItems = new[]
        {
            new LineItem { Amount = 100, Discount = 10 },
            new LineItem { Amount = 50, Discount = 5 }
        },
        ProviderId = "provider",
        PayerId = "payer"
    };

    [Fact]
    public async Task IncomingWorker_RejectsDuplicateAndNonexistentResubmissions()
    {
        var hub = new Mock<IEventHubService>();
        var claims = new Mock<IClaimRepository>();
        var providers = new Mock<IProviderRepository>();
        var payers = new Mock<IPayerRepository>();
        var logger = new Mock<ILogger<EventHubWorkerIncomingClaims>>();
        claims.Setup(x => x.GetClaim("duplicate", null)).ReturnsAsync(new ClaimDetail());
        claims.Setup(x => x.GetClaim("missing", null)).ReturnsAsync((ClaimDetail)null);
        var worker = new IncomingWorker(logger.Object, hub.Object, claims.Object, providers.Object, payers.Object);

        await worker.Process(Message(Detail("duplicate")));
        await worker.Process(Message(Detail("missing", ClaimStatus.Resubmitted)));

        hub.Verify(x => x.TriggerEventAsync(It.IsAny<ClaimDetail>(), Constants.EventHubTopics.Rejected), Times.Exactly(2));
        claims.Verify(x => x.UpdateClaim(It.IsAny<ClaimDetail>()), Times.Never);
        claims.Verify(x => x.CreateClaim(It.IsAny<ClaimDetail>()), Times.Never);
    }

    [Fact]
    public async Task IncomingWorker_UpdatesResubmittedClaimAndCreatesNewClaim()
    {
        var hub = new Mock<IEventHubService>();
        var claims = new Mock<IClaimRepository>();
        var providers = new Mock<IProviderRepository>();
        var payers = new Mock<IPayerRepository>();
        providers.Setup(x => x.GetProvider("provider")).ReturnsAsync(new Provider { Name = "Provider Name" });
        payers.Setup(x => x.GetPayer("payer")).ReturnsAsync(new Payer { Name = "Payer Name" });
        var existing = new ClaimDetail
        {
            ClaimId = "update", AdjudicatorId = "adj", CreatedBy = "original",
            CreatedOn = new DateTime(2023, 1, 1)
        };
        claims.Setup(x => x.GetClaim("update", null)).ReturnsAsync(existing);
        claims.Setup(x => x.GetClaim("new", null)).ReturnsAsync((ClaimDetail)null);
        claims.Setup(x => x.UpdateClaim(It.IsAny<ClaimDetail>())).ReturnsAsync((ClaimDetail x) => new ClaimHeader(x));
        claims.Setup(x => x.CreateClaim(It.IsAny<ClaimDetail>())).ReturnsAsync((ClaimDetail x) => new ClaimHeader(x));
        var worker = new IncomingWorker(logger: new Mock<ILogger<EventHubWorkerIncomingClaims>>().Object,
            hub.Object, claims.Object, providers.Object, payers.Object);

        await worker.Process(Message(Detail("update", ClaimStatus.Resubmitted)));
        await worker.Process(Message(Detail("new")));

        claims.Verify(x => x.UpdateClaim(It.Is<ClaimDetail>(c =>
            c.AdjudicatorId == "adj" && c.CreatedBy == "original" &&
            c.PayerName == "Payer Name" && c.ProviderName == "Provider Name" &&
            c.TotalAmount == 135)), Times.Once);
        claims.Verify(x => x.CreateClaim(It.Is<ClaimDetail>(c =>
            c.ClaimStatus == ClaimStatus.Initial && c.TotalAmount == 135 &&
            c.PayerName == "Payer Name" && c.ProviderName == "Provider Name")), Times.Once);
    }

    [Fact]
    public async Task AdjudicatorChangedWorker_DeletesExistingClaimOnly()
    {
        var hub = new Mock<IEventHubService>();
        var adjudicators = new Mock<IAdjudicatorRepository>();
        var logger = new Mock<ILogger<EventHubWorkerAdjudicatorChanged>>();
        var proposed = new ClaimHeader { ClaimId = "claim", AdjudicatorId = "old" };
        adjudicators.Setup(x => x.GetAdjudicatorClaim("old", proposed.Id)).ReturnsAsync(proposed);
        var worker = new ChangedWorker(logger.Object, hub.Object, adjudicators.Object);

        await worker.Process(JsonSerializationHelper.SerializeItem(proposed));
        adjudicators.Verify(x => x.DeleteAdjudicatorClaim("old", proposed.Id), Times.Once);
    }

    [Fact]
    public async Task AdjudicatorChangedWorker_LeavesMissingClaimAlone()
    {
        var adjudicators = new Mock<IAdjudicatorRepository>();
        var proposed = new ClaimHeader { ClaimId = "claim", AdjudicatorId = "old" };
        adjudicators.Setup(x => x.GetAdjudicatorClaim("old", proposed.Id)).ReturnsAsync((ClaimHeader)null);
        var worker = new ChangedWorker(new Mock<ILogger<EventHubWorkerAdjudicatorChanged>>().Object,
            new Mock<IEventHubService>().Object, adjudicators.Object);

        await worker.Process(JsonSerializationHelper.SerializeItem(proposed));
        adjudicators.Verify(x => x.DeleteAdjudicatorClaim(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangeFeedWorker_StartsConfiguredProcessor()
    {
        var service = new Mock<ICosmosDbChangeFeedService>();
        var worker = new ChangeFeed(new Mock<ILogger<ChangeFeedWorker>>().Object, service.Object);
        await worker.Run();
        service.Verify(x => x.StartChangeFeedProcessorsAsync(), Times.Once);
    }
}
