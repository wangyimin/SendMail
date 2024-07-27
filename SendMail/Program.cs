using System;
using System.Collections.Generic;
using System.Text;

namespace SendMail
{
    class Program
    {
        static void Main(string[] args)
        {
            Mail m = new Mail("王<xxx@yyy.com>", "あいうえお", "あいうえお");
            m.Send(new String[] { "EHLO", "STARTTLS", "AuthPlain", "MailFrom", "RcptTo", "DATA", "QUIT" });
        }
    }
}
