using System;

namespace EAVFW.Extensions.DigitalSigning.Actions
{
    //https://local-hafnia-loimanagement:44325/callbacks/docusign?code=eyJ0eXAiOiJNVCIsImFsZyI6IlJTMjU2Iiwia2lkIjoiNjgxODVmZjEtNGU1MS00Y2U5LWFmMWMtNjg5ODEyMjAzMzE3In0.AQsAAAABAAYABwAAc8oTiebbSAgAAP9QW4nm20gCANef7gNqhp9KiaxaBWOIA74VAAEAAAAYAAEAAAAdAAAADQAkAAAAMjJkNGFkMDctMWZmZC00MDkwLTlmNGEtZWNiZjdjNGFlMTNiIgAkAAAAMjJkNGFkMDctMWZmZC00MDkwLTlmNGEtZWNiZjdjNGFlMTNiMAAAc8oTiebbSBIAAQAAAAsAAABpbnRlcmFjdGl2ZTcANYOE-AH4-0GjX935eDmflg.nAIzMStw9_be-Fp1AckC0sUZ7MCITl7ouNW5evz4YES-SeIeYpkyUW57GkJJZRFxQrii5oP4u3EkEBbgaZ0uf148eYrr2BT-AziK_VJGLkA0xMBHmjQkOECRZF2mK3F9gNEPnbglqTDpC-3d7xnSH5ep9rslDkJD_tCKLhbEtMeYqeTa4e7KNuF_GNMP7JB2D5GBbsXPH78bHEzVIICFQNPzELQW-upDlTtRKhbQ9TbR3v6qlCQgSLx_B5KSNrGXpRl6slgEQzuGRnWGY8FVKhi6BL33AuCmtCTNdmOAlvsM7dGdelVbK3pUvQcUl3M1r12MY1AW4VolbHDXdGWb9w


    public class Base64UrlEncoder
    {
        public string Base64UrlEncode(byte[] arg)
        {
            string s = Convert.ToBase64String(arg); // Regular base64 encoder
            s = s.Split('=')[0]; // Remove any trailing '='s
            s = s.Replace('+', '-'); // 62nd char of encoding
            s = s.Replace('/', '_'); // 63rd char of encoding
            return s;
        }

        public byte[] Base64UrlDecode(string arg)
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
    }
}
