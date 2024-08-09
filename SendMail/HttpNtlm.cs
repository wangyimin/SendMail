using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace SendMail
{
    class HttpNtlm
    {
        private static System.Net.NetworkCredential nc;

        static async Task Authenticate(String uri, bool useNtlm = true)
        {
            //var handler = new SocketsHttpHandler();
            //var client = new HttpClient(handler);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "*/*");

            var ntlm = new Ntlm(nc);
            string msg = ntlm.CreateNegotiateMessage(spnego: !useNtlm);
            //WANG
#if LOG
            Console.WriteLine("Type1:" + msg);
#endif 
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            message.Headers.Add("Authorization", ntlm.CreateNegotiateMessage(spnego: !useNtlm));
            //WANG
            //HttpResponseMessage response = await client.SendAsync(message, default);
            HttpResponseMessage response = await client.SendAsync(message);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                foreach (AuthenticationHeaderValue header in response.Headers.WwwAuthenticate)
                {
                    string blob = ntlm.ProcessChallenge(header);
#if LOG
                    //WANG
                    Console.WriteLine("Type3:" + blob);
#endif

                    if (!string.IsNullOrEmpty(blob))
                    {
                        message = new HttpRequestMessage(HttpMethod.Get, uri);
                        message.Headers.Add("Authorization", blob);
                        //WANG
                        //response = await client.SendAsync(message, default);
                        response = await client.SendAsync(message);
                    }
                }
            }

            Console.WriteLine(response);
            String content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(content);
        }

        public void Execute()
        {
            string uri = ConfigurationManager.AppSettings["URI"];
            string env = Environment.GetEnvironmentVariable("CREDENTIALS");

            if (String.IsNullOrEmpty(env))
            {
                // lame credentials. cab be updated for testing.
                nc = new NetworkCredential(ConfigurationManager.AppSettings["NTLMUSER"],
                    ConfigurationManager.AppSettings["NTLMPASS"], ConfigurationManager.AppSettings["NTLMDOMAIN"]);
            }
            else
            {
                // assume domain\user:password
                string[] part1 = env.Split(new char[] { ':' }, 2);
                string[] part2 = part1[0].Split(new char[] { '\\' }, 2);
                if (part2.Length == 1)
                {
                    nc = new NetworkCredential(part1[0], part1[1]);
                }
                else
                {
                    nc = new NetworkCredential(part2[1], part1[1], part2[0]);
                }
            }

            var client = new HttpClient();
            //WANG
            //HttpResponseMessage probe = await client.GetAsync(uri, CancellationToken.None);
            HttpResponseMessage probe = client.GetAsync(uri, CancellationToken.None).Result;

            if (probe.StatusCode == HttpStatusCode.Unauthorized)
            {
                bool canDoNtlm = false;
                bool canDoNegotiate = false;

                foreach (AuthenticationHeaderValue header in probe.Headers.WwwAuthenticate)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(header.Scheme, "NTLM"))
                    {
                        canDoNtlm = true;
                    }
                    else if (StringComparer.OrdinalIgnoreCase.Equals(header.Scheme, "Negotiate"))
                    {
                        canDoNegotiate = true;
                    }
                    else
                    {
                        Console.WriteLine($"{uri} offers {header.Scheme} authentication");
                    }
                }

                Console.WriteLine("{0} {1} do NTLM authentication", uri, canDoNtlm ? "can" : "cannot");
                Console.WriteLine("{0} {1} do Negotiate authentication", uri, canDoNegotiate ? "can" : "cannot");

                if (canDoNtlm)
                {
                    try
                    {
                        //WANG
                        //await Authenticate(uri, true);
                        Authenticate(uri, true).Wait();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("NTLM Authentication failed");
                        Console.WriteLine(ex);
                    }
                }
                /*
                if (canDoNegotiate)
                {
                    try
                    {
                        //WANG
                        //await Authenticate(uri, false);
                        Authenticate(uri, false).Wait();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Negotiate Authentication failed");
                        Console.WriteLine(ex);
                    }
                }
                */
            }
            else
            {
                Console.WriteLine($"{uri} did not ask for authentication.");
                Console.WriteLine(probe);
            }
        }
    }
}

