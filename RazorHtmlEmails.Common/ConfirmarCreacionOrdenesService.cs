﻿using GComFuelManager.Shared.DTOs;
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
    public class ConfirmarCreacionOrdenesService : EmailSendService, IConfirmarCreacionOrdenes
    {
        private readonly IRazorViewToStringRenderer razorView;
        public ConfirmarCreacionOrdenesService(IRazorViewToStringRenderer razorView)
        {
            this.razorView = razorView;
        }

        public async Task Confirmar(EmailContent<OrdenCierre> content)
        {
            string body = await razorView.RenderViewToStringAsync("./Views/Emails/ConfirmarCreacionOrdenes/ConfirmarCreacionOrdenes.cshtml", content);
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Gcom Fuel Management", "admon@energasmx.mx"));
            message.To.AddRange(content.ToList);
            message.Cc.AddRange(content.CC);
            message.Subject = content.Subject;

            message.Body = new TextPart(TextFormat.Html) { Text = body };

            SendEmail(message);
        }
    }

    public interface IConfirmarCreacionOrdenes
    {
        Task Confirmar(EmailContent<OrdenCierre> cierre);
    }
}
