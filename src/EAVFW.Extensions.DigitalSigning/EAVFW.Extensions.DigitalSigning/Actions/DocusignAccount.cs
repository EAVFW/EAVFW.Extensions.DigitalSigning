using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EAVFW.Extensions.DigitalSigning.Actions
{
    public class DocusignAccount
    {
        [JsonPropertyName("account_id")]
        [JsonProperty("account_id")]
        [DataMember(Name ="account_id")]
        public string AccountId { get; set; }

        [JsonPropertyName("is_default")]
        [JsonProperty("is_default")]
        [DataMember(Name = "is_default")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("account_name")]
        [JsonProperty("account_name")]
        [DataMember(Name = "account_name")]
        public string AccountName { get; set; }

        [JsonPropertyName("base_uri")]
        [JsonProperty("base_uri")]
        [DataMember(Name = "base_uri")]
        public string BaseUrl { get; set; }
    }
}
