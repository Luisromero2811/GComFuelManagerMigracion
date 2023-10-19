using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorHtmlEmails.Common
{
    public class EmailSendService
    {
        internal static void SendEmail(MimeMessage message)
        {
            using (SmtpClient smtpClient = new SmtpClient()
            {
                //solo para pruebas -- remover despues de pruebas
                ServerCertificateValidationCallback = (_, _, _, _) => true
            })
            {
                smtpClient.Connect("smtp.office365.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                smtpClient.Authenticate("admon@energasmx.mx", "Temporal23+");
                smtpClient.Send(message);

                smtpClient.Disconnect(true);
            };
        }
    }
}
