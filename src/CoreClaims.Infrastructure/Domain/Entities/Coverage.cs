using Newtonsoft.Json;

namespace CoreClaims.Infrastructure.Domain.Entities
{
    public class Coverage : BaseEntity
    {
        public const string EntityName = nameof(Coverage);

        public Coverage()
        {
            Type = EntityName;
        }

        [JsonProperty("id")]
        public override string Id => $"coverage:{CoverageId}";

        [JsonProperty("coverageId")]
        public string CoverageId { get; set; } = null!;

        [JsonProperty("memberId")]
        public string MemberId { get; set; } = null!;

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime EndDate { get; set; }

        [JsonProperty("payerId")]
        public string PayerId { get; set; } = null!;
    }
}
