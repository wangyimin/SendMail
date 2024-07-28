using System;

namespace SendMail
{
    class Program
    {
        static void Main(string[] args)
        {
            Mail m = new Mail
            {
                to = "王<xxx@yyy.com>",
                subject = "あいうえお",
                body = "あいうえお",
                attachment = @"d:\Wang\テスト.XLSX"
            };

            m.Send(new String[] { "EHLO", "STARTTLS", "AuthPlain", "MailFrom", "RcptTo", "DATA", "QUIT" });
            
        }
    }
}
