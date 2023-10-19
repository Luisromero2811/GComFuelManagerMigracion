using GComFuelManager.Shared.DTOs;
using GComFuelManager.Shared.Modelos;
using MimeKit.Text;
using MimeKit;
using RazorHtmlEmails.GComFuelManagerMigracion.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorHtmlEmails.Common
{
    public class ConfirmOrden : EmailSendService, IConfirmOrden
    {
        private readonly IRazorViewToStringRenderer razorView;

        public ConfirmOrden(IRazorViewToStringRenderer razorView)
        {
            this.razorView = razorView;
        }

        public async Task NotifyConfirmOrden(EmailContent<OrdenEmbarque> content)
        {
            string body = await razorView.RenderViewToStringAsync("./Views/Emails/ConfirmOrdens/ConfirmOrdens.cshtml", content);
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Gcom Fuel Management", "admon@energasmx.mx"));
            message.To.AddRange(content.ToList);
            message.Cc.AddRange(content.CC);
            message.Subject = content.Subject;

            message.Body = new TextPart(TextFormat.Html) { Text = body };

            SendEmail(message);
        }
    }

    public interface IConfirmOrden
    {
        Task NotifyConfirmOrden(EmailContent<OrdenEmbarque> content);
    }
}
