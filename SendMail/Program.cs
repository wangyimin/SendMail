using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace SendMail
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            using (SmtpClient client = new SmtpClient())
            { 
                MailAddress from = new MailAddress("<from>");
                MailAddress to = new MailAddress("<to>");
                using (MailMessage message = new MailMessage(from, to)
                {
                    Subject = "Subject",
                    Body = "Body",
                })
                {

                    message.Body = "Body";
                    message.Subject = "Subject";
                    client.Send(message);
                }
            }
            */

            /*
            Mail m = new Mail
            {
                to = "王<to>",
                subject = "あいうえお",
                body = "あいうえお",
                attachment = @"d:\Wang\テスト.XLSX"
            };

            //m.Send(args[0].Split(','));
            m.Send(new String[] { "EHLO", "STARTTLS", "AuthPlain", "MailFrom", "RcptTo", "DATA", "QUIT" });
            */

            new HttpNtlm().Execute();
            Console.WriteLine("OK");
        }
    }
}
