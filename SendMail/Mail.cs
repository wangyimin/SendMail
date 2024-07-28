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
        private static readonly string MULTIPLE_PARTS = "_MIME-Boundary";
        private static readonly string SERVER = ConfigurationManager.AppSettings["SERVER"];
        private static readonly int PORT = Int32.Parse(ConfigurationManager.AppSettings["PORT"]);
        private static readonly string USER  = ConfigurationManager.AppSettings["USER"];
        private static readonly string PASSWORD = ConfigurationManager.AppSettings["PASSWORD"];

        private static readonly  Encoding ENC = System.Text.Encoding.GetEncoding("ISO-2022-JP");
        private static readonly String[] CORRECT_COMMANDS = new String[] {
            "EHLO", "STARTTLS", "AuthPlain", "MailFrom", "RcptTo", "DATA", "QUIT" };
   
        private static readonly string from = ConfigurationManager.AppSettings["FROM"];
        public string to;
        public string subject;
        public string body;
        public string attachment;


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

                        MethodInfo m = this.GetType()
                            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                            .Single(el => el.Name == "Send" + command && el.IsGenericMethodDefinition)
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
                    Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                    throw ex;
                }
                finally
                {
                    sslStream?.Close();
                    netStream?.Close();
                    client?.Close();
                }
            }
        }

        private void SendEHLO<T>(T stream) where T : Stream => StreamWriteAndRead(stream, "EHLO localhost" + Environment.NewLine, "2");
        private void SendSTARTTLS<T>(T stream) where T : Stream => StreamWriteAndRead(stream, "STARTTLS" + Environment.NewLine, "2");
        private void SendAuthPlain<T>(T stream) where T : Stream =>
            StreamWriteAndRead(stream, "AUTH PLAIN " + GetEncode64("\0" + USER + "\0" + PASSWORD, true) + Environment.NewLine, "2");
        private void SendMailFrom<T>(T stream) where T : Stream =>
            StreamWriteAndRead(stream, "MAIL FROM:<" + new System.Net.Mail.MailAddress(from).Address + ">" + Environment.NewLine, "2");
        private void SendRcptTo<T>(T stream) where T : Stream => 
            StreamWriteAndRead(stream, "RCPT TO:<" + new System.Net.Mail.MailAddress(to).Address + ">" + Environment.NewLine, "2");
        private void SendDATA<T>(T stream) where T : Stream
        {
            StreamWriteAndRead(stream, "DATA" + Environment.NewLine, "3");
            SendDataContent(stream);
        }
        private void SendQUIT<T>(T stream) where T : Stream => StreamWriteAndRead(stream, "QUIT" + Environment.NewLine, "2");

        private void SendDataContent<T>(T stream) 
            where T : Stream
        {
            String data = "";

            // Header:MIME-Version
            data += "MIME-Version: 1.0" + Environment.NewLine;

            // Header:From
            data += "From: " + GetEncode64(new System.Net.Mail.MailAddress(from).DisplayName) + 
                "<" + new System.Net.Mail.MailAddress(from).Address + ">" + Environment.NewLine;

            // Header:To
            data += "To: " + GetEncode64(new System.Net.Mail.MailAddress(to).DisplayName) + 
                "<" + new System.Net.Mail.MailAddress(to).Address + ">" + Environment.NewLine;

            // Header:Subject
            data += "Subject: " + GetEncode64(subject) + Environment.NewLine;

            data += "content-type: multipart/mixed; boundary=\"" + MULTIPLE_PARTS + "\"" + Environment.NewLine + Environment.NewLine;
            // Body
            data += "--" + MULTIPLE_PARTS + Environment.NewLine;
            data += "Content-Type: text/plain; charset=\"" + ENC.BodyName + "\"" + Environment.NewLine;
            data += "Content-Transfer-Encoding: 7bit" + Environment.NewLine + Environment.NewLine;
            data += body + Environment.NewLine;

            // Attachment
            data += CreateAttachemtnData();

            // .->..(RFC2821:period is first character of the line)
            data = data.Replace(Environment.NewLine + "." + Environment.NewLine, Environment.NewLine + ".." + Environment.NewLine);

            // Completed
            data += Environment.NewLine + "." + Environment.NewLine;
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

        private String CreateAttachemtnData()
        {
            if (String.IsNullOrEmpty(attachment)) return "";

            String r = "";
            r += "--" + MULTIPLE_PARTS + Environment.NewLine;
            r += "content-type: application/octet-stream; name=" + GetEncode64(Path.GetFileName(attachment)) + Environment.NewLine;
            r += "content-transfer-encoding: base64" + Environment.NewLine + Environment.NewLine;
            r += Convert.ToBase64String(File.ReadAllBytes(attachment)) + Environment.NewLine;
            return r;
        }
    }
}
