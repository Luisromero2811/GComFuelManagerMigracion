using MimeKit.Text;
using MimeKit;
using RazorHtmlEmails.GComFuelManagerMigracion.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RazorHtmlEmails.GComFuelManagerMigracion.Views.Emails.ConfirmationAccount;
using MailKit.Net.Smtp;
using MailKit.Security;
using GComFuelManager.Shared.DTOs;
using MimeKit.Utils;
using System.Net.Mail;
using System.Net.Mime;
using GComFuelManager.Shared.Modelos;

namespace RazorHtmlEmails.Common
{
    public class CompraService : EmailSendService, ICompraService
    {
        private readonly IRazorViewToStringRenderer razorView;

        public CompraService(IRazorViewToStringRenderer razorView)
        {
            this.razorView = razorView;
        }

        public async Task Comprar(EmailContent<OrdenCierre> content)
        {

            string body = await razorView.RenderViewToStringAsync("./Views/Emails/ConfirmationAccount/ConfirmaAccount.cshtml", content);
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Gcom Fuel Management", "admon@energasmx.mx"));
            message.To.Add(new MailboxAddress(content.Nombre, content.Email));
            message.Cc.AddRange(content.CC);
            message.Subject = content.Subject;

            message.Body = new TextPart(TextFormat.Html) { Text = body };
            
            SendEmail(message);
        }

    }

    public interface ICompraService
    {
        Task Comprar(EmailContent<OrdenCierre> content);
    }
}
