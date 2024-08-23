using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SendMail
{
    class OAuth2
    {

        public static readonly String URI = ConfigurationManager.AppSettings["OAUTH2_URI"];
        public static readonly String clientId = ConfigurationManager.AppSettings["OAUTH2_CLIENTID"];
        public static readonly String clientSecret = ConfigurationManager.AppSettings["OAUTH2_CLIENTSECRET"];
        public static readonly String refreshToken = ConfigurationManager.AppSettings["OAUTH2_REFRESHTOKEN"];
        public static String tagName = "access_token";

        public Func<String, String> parser = s =>
            (JsonSerializer.Deserialize<Dictionary<string, string>>(s))[tagName];
 
        public async Task<String> GetAccessToken()
        {
            using (HttpClient hc = new HttpClient())
            {
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, URI);
                hrm.Headers.Add("client_id", clientId);
                hrm.Headers.Add("client_secret", clientSecret);
                hrm.Headers.Add("refresh_token", refreshToken);
                hrm.Headers.Add("grant_type", "refresh_token");

                HttpResponseMessage resp = await hc.SendAsync(hrm);
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    String content = await resp.Content.ReadAsStringAsync();
                    return parser(content);
                }
                else
                {
                    throw new InvalidOperationException("Unknown error.");
                }
            }
        }
    }
}
