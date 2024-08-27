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
           JsonDocument.Parse(s)
                .RootElement
                .GetProperty(tagName)
                .GetString();

        public async Task<String> GetAccessToken()
        {
            using (HttpClient hc = new HttpClient())
            {
                using (HttpResponseMessage resp = await hc.PostAsync(URI,
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "refresh_token",
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["refresh_token"] = refreshToken,
                    })))
                { 
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
}
