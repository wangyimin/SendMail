using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Configuration;

namespace SendMail
{
    class Mail
    {
        private static readonly string SERVER = ConfigurationManager.AppSettings["SERVER"];
        private static readonly int PORT = Int32.Parse(ConfigurationManager.AppSettings["PORT"]);
        private static readonly string USER  = ConfigurationManager.AppSettings["USER"];
        private static readonly string PASSWORD = ConfigurationManager.AppSettings["PASSWORD"];

        private static readonly  Encoding ENC = System.Text.Encoding.GetEncoding("ISO-2022-JP");
        private static readonly String[] CORRECT_COMMANDS = new String[] {
            "EHLO", "STARTTLS", "AuthPlain", "MailFrom", "RcptTo", "DATA", "QUIT" };
   
        private readonly string from = ConfigurationManager.AppSettings["FROM"];
        private readonly string to;
        private readonly string subject;
        private readonly string body;

        public Mail(String to, String subject, String body)
        {
            this.to = to;
            this.subject = subject;
            this.body = body;
        }

        public void Send(IEnumerable<String> commands)
        {
            System.Net.Sockets.NetworkStream netStream = null;
            System.Net.Security.SslStream sslStream = null;
            Stream stream = null;

            using (System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient())
            {
                try
                {
                    // Connection established
                    client.Connect(SERVER, PORT);
                    stream = netStream = client.GetStream();
                    StreamWriteAndRead(stream, "", "2");

                    foreach (String command in commands)
                    {
                        if (Array.IndexOf(CORRECT_COMMANDS, command) == -1)
                            throw new ArgumentException("Invalid command![" + command + "]");

                        String method = "Send" + command;
                        MethodInfo m = this.GetType()
                            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                            .Single(el => el.Name == method && el.IsGenericMethodDefinition)
                            .MakeGenericMethod(stream.GetType());
                        m.Invoke(this, new Object[] { stream });

                        if (command.Equals("STARTTLS"))
                        {
                            stream = sslStream = new System.Net.Security.SslStream(netStream);
                            sslStream.AuthenticateAsClient(SERVER);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                    throw ex;
                }
                finally
                {
                    netStream?.Close();
                    sslStream?.Close();
                }
            }
        }

        private void SendEHLO<T>(T stream) where T : Stream => StreamWriteAndRead(stream, "EHLO localhost\r\n", "2");
        private void SendSTARTTLS<T>(T stream) where T : Stream => StreamWriteAndRead(stream, "STARTTLS\r\n", "2");
        private void SendAuthPlain<T>(T stream) where T : Stream =>
            StreamWriteAndRead(stream, "AUTH PLAIN " + GetEncode64("\0" + USER + "\0" + PASSWORD, true) + "\r\n", "2");
        private void SendMailFrom<T>(T stream) where T : Stream =>
            StreamWriteAndRead(stream, "MAIL FROM:<" + new System.Net.Mail.MailAddress(from).Address + ">\r\n", "2");
        private void SendRcptTo<T>(T stream) where T : Stream => 
            StreamWriteAndRead(stream, "RCPT TO:<" + new System.Net.Mail.MailAddress(to).Address + ">\r\n", "2");
        private void SendDATA<T>(T stream) where T : Stream
        {
            StreamWriteAndRead(stream, "DATA\r\n", "3");
            SendDataContent(stream);
        }
        private void SendQUIT<T>(T stream) where T : Stream => StreamWriteAndRead(stream, "QUIT\r\n", "2");

        private void SendDataContent<T>(T stream) 
            where T : Stream
        {
            String data = "";
            // Header:From
            data += "From: " + GetEncode64(new System.Net.Mail.MailAddress(from).DisplayName) + 
                "<" + new System.Net.Mail.MailAddress(from).Address + ">\r\n";

            // Header:To
            data += "To: " + GetEncode64(new System.Net.Mail.MailAddress(to).DisplayName) + 
                "<" + new System.Net.Mail.MailAddress(to).Address + ">\r\n";

            // Header:Subject
            data += "Subject: " + GetEncode64(subject) + "\r\n";

            // Header:MIME-Version
            data += "MIME-Version: 1.0\r\n";

            // Header:Content-Type
            data += "Content-Type: text/plain; charset=\"" + ENC.BodyName + "\"\r\n";

            // Header:Content-Transfer-Encoding
            data += "Content-Transfer-Encoding: 7bit\r\n";

            data += "\r\n";

            // Body
            data += body + "\r\n";

            // .->..(RFC2821:period is first character of the line)
            data = data.Replace("\r\n.\r\n", "\r\n..\r\n");

            // Completed
            data += "\r\n.\r\n";
            StreamWriteAndRead(stream, data, "2");
        }

        private String StreamWriteAndRead<T>(T stream, String req, String okCode)
            where T : Stream
        {
            byte[] buff;
            // request
            if (req != "")
            {
                Console.Write(req);
                buff = ENC.GetBytes(req);
                stream.Write(buff, 0, buff.Length);
                stream.Flush();
            }

            // response
            buff = new byte[2048];
            int l = stream.Read(buff, 0, buff.Length);
            string resp = ENC.GetString(buff, 0, l);
            Console.Write(resp);

            if (!resp.StartsWith(okCode)) throw new InvalidOperationException(resp);
            return resp;
        }

        private String GetEncode64(String s, bool isPassphrase = false)
        {
            if (s == "") return "";
            return isPassphrase ? Convert.ToBase64String(ENC.GetBytes(s)) :
                "=?" + ENC.BodyName + "?B?" + Convert.ToBase64String(ENC.GetBytes(s)) + "?=";
        }
    }
}
