﻿using MimeKit.Text;
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

namespace RazorHtmlEmails.Common
{
    public class RegisterAccountService : EmailSendService, IRegisterAccountService
    {
        private readonly IRazorViewToStringRenderer razorView;

        public RegisterAccountService(IRazorViewToStringRenderer razorView)
        {
            this.razorView = razorView;
        }

        public async Task Register(EmailContent content)
        {

            string body = await razorView.RenderViewToStringAsync("/Views/Emails/ConfirmationAccount/ConfirmaAccount.cshtml", content);
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Gcom Fuel Manager", "endpoint@gasamigas.com"));
            message.To.Add(new MailboxAddress(content.Nombre, content.Email));
            message.Cc.AddRange(content.CC);
            message.Subject = content.Subject;

            message.Body = new TextPart(TextFormat.Html) { Text = body };

            SendEmail(message);
        }

    }

    public interface IRegisterAccountService
    {
        Task Register(EmailContent content);
    }
}