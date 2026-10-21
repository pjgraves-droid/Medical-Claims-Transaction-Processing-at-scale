using CoreClaims.Infrastructure.Domain.Entities;
using CoreClaims.SemanticKernel;
using Microsoft.Extensions.AI;

namespace CoreClaims.Tests;

public class RulesEngineTests
{
    private sealed class FakeChatClient(string reply) : IChatClient
    {
        public List<ChatMessage> Messages { get; } = [];
        public ChatOptions? Options { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Messages.AddRange(messages);
            Options = options;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    private static ClaimDetail SampleClaim() => new()
    {
        ClaimId = "claim-123",
        MemberId = "member-456",
        PayerName = "Contoso Health",
        ProviderName = "Fabrikam Clinic",
        TotalAmount = 1234.5m,
        FilingDate = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc),
        LineItems = [],
    };

    [Fact]
    public async Task ReviewClaim_ReturnsModelResponseText()
    {
        var chat = new FakeChatClient("Approve: no rules violated.");

        var result = await new RulesEngine(chat).ReviewClaim(SampleClaim());

        Assert.Equal("Approve: no rules violated.", result);
    }

    [Fact]
    public async Task ReviewClaim_SendsSingleUserPromptContainingClaimJsonAndRules()
    {
        var chat = new FakeChatClient("ok");

        await new RulesEngine(chat).ReviewClaim(SampleClaim());

        var message = Assert.Single(chat.Messages);
        Assert.Equal(ChatRole.User, message.Role);
        Assert.Contains("insurance claim review agent", message.Text);
        Assert.Contains("\"ClaimId\": \"claim-123\"", message.Text);
        Assert.Contains("\"PayerName\": \"Contoso Health\"", message.Text);
        Assert.DoesNotContain("{{claim}}", message.Text);
    }

    [Fact]
    public async Task ReviewClaim_UsesDeterministicChatOptions()
    {
        var chat = new FakeChatClient("ok");

        await new RulesEngine(chat).ReviewClaim(SampleClaim());

        Assert.NotNull(chat.Options);
        Assert.Equal(2000, chat.Options!.MaxOutputTokens);
        Assert.Equal(0.0f, chat.Options.Temperature);
    }
}
