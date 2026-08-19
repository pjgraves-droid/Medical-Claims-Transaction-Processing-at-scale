using Newtonsoft.Json;

namespace CoreClaims.Infrastructure.Domain.Entities
{
    public class Provider : BaseEntity
    {
        public const string EntityName = nameof(Provider);

        public Provider()
        {
            Type = EntityName;
        }

        [JsonProperty("id")]
        public override string Id => ProviderId;

        [JsonProperty("providerId")]
        public string ProviderId { get; set; } = null!;

        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("email")]
        public string Email { get; set; } = null!;

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; } = null!;

        [JsonProperty("address")]
        public string Address { get; set; } = null!;

        [JsonProperty("city")]
        public string City { get; set; } = null!;

        [JsonProperty("state")]
        public string State { get; set; } = null!;

        [JsonProperty("country")]
        public string Country { get; set; } = null!;
    }
}
