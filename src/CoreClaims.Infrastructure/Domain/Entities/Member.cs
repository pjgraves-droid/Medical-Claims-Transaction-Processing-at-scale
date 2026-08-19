using CoreClaims.Infrastructure.Domain.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CoreClaims.Infrastructure.Domain.Entities
{
    public class Member : BaseEntity
    {
        public const string EntityName = nameof(Member);

        public Member()
        {
            Type = EntityName;
        }

        [JsonProperty("id")]
        public override string Id => MemberId;

        [JsonProperty("memberId")]
        public string MemberId { get; set; } = null!;

        [JsonProperty("memberType")]
        //[JsonConverter(typeof(StringEnumConverter))]
        public string MemberType { get; set; } = null!;
        //public MemberTypeEnum MemberType { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = null!;

        [JsonProperty("firstName")]
        public string FirstName { get; set; } = null!;

        [JsonProperty("lastName")]
        public string LastName { get; set; } = null!;

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

        [JsonProperty("approvedCount")]
        public int ApprovedCount { get; set; }

        [JsonProperty("approvedTotal")]
        public decimal ApprovedTotal { get; set; }
    }
}
