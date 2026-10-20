using Azure.Core;
using Azure.Identity;

namespace CoreClaims.Infrastructure.Identity
{
    public static class AzureCredentialFactory
    {
        public static TokenCredential Create(string clientId)
        {
#if DEBUG
            return new DefaultAzureCredential();
#else
            return new ChainedTokenCredential(
                new ManagedIdentityCredential(clientId),
                new AzureCliCredential());
#endif
        }
    }
}
