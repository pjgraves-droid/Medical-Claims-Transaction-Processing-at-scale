using Microsoft.Azure.Cosmos;
using Moq;

namespace CoreClaims.Tests;

internal static class TestCosmosHelpers
{
    public static FeedIterator<T> Iterator<T>(IEnumerable<T> items)
    {
        var response = new Mock<FeedResponse<T>>();
        response.SetupGet(x => x.Resource).Returns(items.ToList());
        response.Setup(x => x.GetEnumerator()).Returns(() => items.GetEnumerator());

        var iterator = new Mock<FeedIterator<T>>();
        iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
        iterator.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response.Object);
        return iterator.Object;
    }
}
