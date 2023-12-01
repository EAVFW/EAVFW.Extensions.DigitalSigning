using DocuSign.eSign.Client;
using EAVFW.Extensions.DigitalSigning.Actions;
using EAVFW.Extensions.DigitalSigning.DocuSign;
using EAVFW.Extensions.DigitalSigning.DocuSign.Configuration;
using EAVFW.Extensions.DigitalSigning.DocuSign.Services;
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
        
        private readonly IMemoryCache _memoryCache;
        private readonly IDigitalSigningAuthContextProtector _digitalSigningAuthContextProtector;

        public DocusignHttpClientHandler(  IMemoryCache memoryCache, IDigitalSigningAuthContextProtector digitalSigningAuthContextProtector)
        {
           
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _digitalSigningAuthContextProtector = digitalSigningAuthContextProtector;
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

            if (request.Options.TryGetValue(DocuSignContext.DigitalSigningContextKey, out var docuSignContext))
            {
                var resttoken = await _memoryCache.GetOrCreateAsync("docusigntoken_" + docuSignContext.SigningProviderID, async (entry) =>
                {

                    var authContext = await _digitalSigningAuthContextProtector.UnprotectAuthContext(docuSignContext.SigningProviderID);

                    var userinfo = authContext.UserInfoResponse;
                    var claims = JsonDocument.Parse(userinfo).RootElement.ToClaims();

                    var sub = claims.Where(c => c.Type == "sub").Select(c => c.Value).FirstOrDefault();



                    entry.SetSize(1);
                    entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(3600));

                    var rsb_private_base64 = authContext.PrivateRSA.Trim();
                    if (rsb_private_base64.StartsWith("-----BEGIN RSA PRIVATE KEY-----"))
                        rsb_private_base64= rsb_private_base64.Substring("-----BEGIN RSA PRIVATE KEY-----".Length);
                    
                    if (rsb_private_base64.EndsWith("-----END RSA PRIVATE KEY-----"))
                        rsb_private_base64= rsb_private_base64.Substring(0,rsb_private_base64.Length - "-----END RSA PRIVATE KEY-----".Length);



                    //var rsb_private_base64 = "MIIEowIBAAKCAQEAuKo3sTVfcGD9JGl765MuJIfFBYAsECBM/OCWOpwBkRoJIUAY\r\nz5rWCRdQOq2/JeZqvlatKk8TbuoxT4eGAbzCe1pWVLoRkQZ4DVhegXgOjPzgoCDM\r\n7nKZXIvqn45NTFDcwiE8dfZuqHH7YhVmDdyKujuRATBtoiHkFE8cBV7R1U4wssko\r\nayVlviHRVl0UGb3POzvfgCjX4QHihyx4BbYgY2cxIBeVmpAS829yNJQZat05KGxm\r\nZsBHM3NKQ898FNkzplL1bzMaNxtQZ+4YIwXDzgHy8ZzomcY7/rWtszmGHAts+s7W\r\nCJ033inUQfyge0avO5Lzs9gB4uPU+co4hIY/mQIDAQABAoIBAAaVCqcmX+CBlIRX\r\nvjMHa/2hWUR47RkmDZh16OTt6qBhbTHiNwAvZLGuQw3UY5vWOrfOHx4C845giqFr\r\nEzu8nrIG5kze1QF2WZEgNT4oKVydJuSORr+5Ff5WfmZxjA/MUhIZXS5XOlqGtY8p\r\ndBnKYqeAuApyz3WX/6k5qKYIWzJfJ5XL2hmHhBXrhZZID351mM6btv7b6H1aRlqx\r\nBPMIiIadHEyQf5ZJPIwJEVYkjU6aIFfNJHf0H4lczRzqJpOkbQOSClBPqZ6w241L\r\nZ03ttKo+a8uq/vC5onjt29qNB5pCSGigA38D8CeYOc234Vzj0Q9O8lGUPQvQTAKT\r\nUZYmsgECgYEA5xuqLXzdCrwwIbK5J+BAAbAuN1uP+mbvbRrkjVNcO1gHf9ZM0JJA\r\n83yRl9iHLS0hunvjspb/r2JuEkMIZVIV0fmAJgZ9jh7KODf2J3DWjWjDPzmFMOrd\r\nvYu0pw8+/JDqvf1qhd61nM+uO+lvmz951+cvE4ocKEvb5zpawJMwkZUCgYEAzI36\r\nK2/b55ADtnSOP6yo15pWr8BVbf8HiCqYnRoUPpAEMfs9KfzSUbxjZrhWMEdwNpKC\r\nGPOh9sbms25yirqLMzWxuhw39zv9j59s6mm+tYY4x6eRS3CZPUqjkf4u5239Up3I\r\nrvR8vNx3712gaKfx40La7iIAYlnzyJxwxA48PPUCgYAFGkhEntgmj4ckh1StO7dO\r\nEjzC/iOMrM8mgox/TlLgAI6R1QJ3LHOArMRuLNq3NaLkAi5B2DGnBq5VeuNpkUlo\r\nDHE25bfJ9oYSfbHSpxwlnSWKdNOrA9SHhdkkQyLp4q08Kqc6c3NhFfjL29iZ/enL\r\nyY/xh4Adp6cgUNqGG/nbjQKBgDQok7zVtgYSw8+XYto7pYXsdeQ0r9PvbrU631VX\r\n3Aej4133ST5WmC59Uf9US63d2XAg5YmFPixVxWfFZvGW5X22WE4zedXR9zLTHQuc\r\n0SMqSXoncTYCTSDC0nicjss2UZzqXMy3zMK/mNlxT0DAaj5fwsFr7BsoArCJq3ti\r\ngdutAoGBAMcnajCGY3WPm/VEgVYOv7YknjVTQypiVWPlL7W+9VQ1S3/RRqRlP5me\r\nnqCsCnIkDhOG1IgrTNpexj2pqqss2I58EerAq3IaaQ41HO4FMtgnbpbYr5GiF12g\r\nobmus4BtllQqzIa75DL2Q0LtB2F15vCyPI29CfmQYE3RewNdfyIb";

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
                    var jwtsignatur = Base64UrlEncode(SignData($"{jwtheader}.{jwtbody}", rsb_private_base64.Trim()));
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
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
