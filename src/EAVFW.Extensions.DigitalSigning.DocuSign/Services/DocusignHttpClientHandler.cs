using EAVFW.Extensions.DigitalSigning.Actions;
using EAVFW.Extensions.DigitalSigning.DocuSign;
using EAVFW.Extensions.DigitalSigning.DocuSign.Configuration;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EAVFW.Extensions.DigitalSigning.Services
{
    public class DocusignHttpClientHandler : DelegatingHandler
    {
        private readonly DocuSignContext docuSignContext;
        private readonly IMemoryCache _memoryCache;
        private readonly IDigitalSigningAuthContextProtector digitalSigningAuthContextProtector;

        public DocusignHttpClientHandler(DocuSignContext docuSignContext, IMemoryCache memoryCache, IDigitalSigningAuthContextProtector digitalSigningAuthContextProtector)
        {
            this.docuSignContext = docuSignContext;
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.digitalSigningAuthContextProtector = digitalSigningAuthContextProtector;
        }

        static string Base64UrlEncode(byte[] arg)
        {
            string s = Convert.ToBase64String(arg); // Regular base64 encoder
            s = s.Split('=')[0]; // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding
            return s;
        }

        static byte[] Base64UrlDecode(string arg)
        {
            string s = arg;
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding
            switch (s.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: s += "=="; break; // Two pad chars
                case 3: s += "="; break; // One pad char
                default:
                    throw new System.Exception(
                  "Illegal base64url string!");
            }
            return Convert.FromBase64String(s); // Standard base64 decoder
        }
        static byte[] SignData(string data, string privateKey)
        {
            byte[] privateKeyBytes = Convert.FromBase64String(privateKey);

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportRSAPrivateKey(privateKeyBytes, out _);

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                return signatureBytes;
            }
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            
            var resttoken = await _memoryCache.GetOrCreateAsync("docusigntoken_"+ docuSignContext.SigningProviderID, async (entry) =>
            {

                var authContext = await digitalSigningAuthContextProtector.UnprotectAuthContext(docuSignContext.SigningProviderID);

                var userinfo = authContext.UserInfoResponse;
                var claims = JsonDocument.Parse(userinfo).RootElement.ToClaims();

                var sub = claims.Where(c => c.Type == "sub").Select(c => c.Value).FirstOrDefault();



                entry.SetSize(1);
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(3));

                var rsb_private_base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(authContext.PrivateRSA));

                var jwtheader = Base64UrlEncode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
                var jwtbody = Base64UrlEncode(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
                {
                    iss = authContext.ClientId,
                    sub = sub,
                    aud = new Uri(authContext.BaseUrl).Host,
                    iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    exp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600,
                    scope = "signature impersonation"
                }));
                var jwtsignatur = Base64UrlEncode(SignData($"{jwtheader}.{jwtbody}", rsb_private_base64));
                var jwt = $"{jwtheader}.{jwtbody}.{jwtsignatur}";
                //jwt = "eyJ0eXAiOiJqd3QiLCJhbGciOiJSUzI1NiJ9.eyJpc3MiOiIyMmQ0YWQwNy0xZmZkLTQwOTAtOWY0YS1lY2JmN2M0YWUxM2IiLCJzdWIiOiIwM2VlOWZkNy04NjZhLTRhOWYtODlhYy01YTA1NjM4ODAzYmUiLCJhdWQiOiJhY2NvdW50LWQuZG9jdXNpZ24uY29tIiwiaWF0IjoxNjg0MjM5MzkwLCJleHAiOjE2ODUyMzkzOTAsInNjb3BlIjoic2lnbmF0dXJlIGltcGVyc29uYXRpb24ifQ.hH1ZNQURNwgqaE6h5R5DxMXbBLwsy6G4PwRNAKeHuaXe_8-UkW9EBRK8sacHP31rV-_Hpf_VrqrFpfD4vc1JmOeTYeWzfjqxBBsGQ30veXmdUMYEE458xG25D0o1xQNbTj1bTM0hkV4CNWct2iyWJsXHl0emQdVsd5ktg_qcyN8Hjw93l8mIM4as-8jebENZp0B0SQrKKUrijsjjkbI4I0KpLkjpFfnEfSF2kJ2TjVk0c3-CHNP1wkVYiCsgw-KebdU0b0DJ3Ax3HFhoirGCpkjnF_LYJ0w9sHTFt36tRiIj3L-NOBZtkYOb2iSR7u9zeMOwBaqB1A7-z7UorsfiMQ";

                var http = new HttpClient();

                var rsp = await http.PostAsync($"{authContext.BaseUrl}/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["assertion"] = jwt,
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer"
                }));

                var tokenresponse = await rsp.Content.ReadAsStringAsync();

                return JToken.Parse(tokenresponse).SelectToken("$.access_token")?.ToString();
            });
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", resttoken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
