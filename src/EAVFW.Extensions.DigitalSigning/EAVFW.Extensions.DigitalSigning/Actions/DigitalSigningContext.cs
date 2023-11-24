using System.Text.Json.Serialization;

namespace EAVFW.Extensions.DigitalSigning.Actions
{
    public class DigitalSigningContext
    {
        [JsonPropertyName("baseUrl")]
        public string BaseUrl { get; set; }
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; }
        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; }

        [JsonPropertyName("redirect_url")]
        public string RedirectUrl { get; set; }

        [JsonPropertyName("token_response")]
        public string TokenResponse { get; set; }
        [JsonPropertyName("userinfo_response")]
        public string UserInfoResponse { get; set; }

        [JsonPropertyName("rsa_private")]
        public string PrivateRSA { get; set; }
    }
}
