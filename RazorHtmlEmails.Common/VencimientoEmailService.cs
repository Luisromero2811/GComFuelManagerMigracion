using MimeKit.Text;
using MimeKit;
using RazorHtmlEmails.GComFuelManagerMigracion.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GComFuelManager.Shared.DTOs;
using System.Net.Mail;
using System.Net.Mime;
using GComFuelManager.Shared.Modelos;

namespace RazorHtmlEmails.Common
{
    public class VencimientoEmailService: EmailSendService, IVencimientoService
    {
        private readonly IRazorViewToStringRenderer razorView;

        public VencimientoEmailService(IRazorViewToStringRenderer razorView)
        {
            this.razorView = razorView;
        }

        public async Task Vencimiento(EmailContent<OrdenCierre> content)
        {
            string body = await razorView.RenderViewToStringAsync("/Views/Emails/Vencimiento/VencimientoPage.cshtml", content);
            var message = new MimeMessage();
            
            //AlternateView alternateView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

            //string url = "./img/gcom_unilogo.png";
            //LinkedResource linkedResource = new LinkedResource(url);
            //linkedResource.ContentId = "logo";
            //alternateView.LinkedResources.Add(linkedResource);

            message.From.Add(new MailboxAddress("Gcom Fuel Management", "admon@energasmx.mx"));
            message.To.Add(new MailboxAddress(content.Nombre, content.Email));
            message.Cc.AddRange(content.CC);
            message.Subject = content.Subject;
            message.Body = new TextPart(TextFormat.Html) { Text = body };

            SendEmail(message);
        }
    }

    public interface IVencimientoService
    {
        Task Vencimiento(EmailContent<OrdenCierre> content);
    }
}
