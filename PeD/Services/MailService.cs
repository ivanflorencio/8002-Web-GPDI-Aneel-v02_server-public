
using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PeD.Core;
using PeD.Views.Email;
using SendGrid.Helpers.Mail;

namespace PeD.Services
{
    public class MailService
    {
        private readonly EmailSettings EmailSettings;
        protected IViewRenderService ViewRender;
        protected ILogger<SendGridService> Logger;
        protected EmailAddress From;
        protected EmailSettings Settings;

        public MailService(IViewRenderService viewRender, EmailSettings settings)
        {
            EmailSettings = settings;
            ViewRender = viewRender;
        }

        public async Task<bool> Send(string to, string subject, string content, string title = null,
            string actionLabel = null, string actionUrl = null)
        {
            return await Send(new[] { to }, subject, content, title, actionLabel, actionUrl);
        }

        public async Task<bool> Send(string[] tosEmail, string subject, string content, string title = null,
            string actionLabel = null, string actionUrl = null)
        {
            title ??= subject;
            return await Send(tosEmail, subject, "Email/SimpleMail",
                new SimpleMail()
                { Titulo = title, Conteudo = content, ActionLabel = actionLabel, ActionUrl = actionUrl });
        }

        public async Task<bool> Send<T>(string[] tosEmail, string subject, string viewName, T model)
        {
            MailMessage message = new MailMessage();
            SmtpClient smtp = new SmtpClient();
            From = new EmailAddress(EmailSettings.Mail, EmailSettings.DisplayName);

            message.From = new MailAddress(EmailSettings.Mail, EmailSettings.DisplayName);
            foreach (var to in tosEmail)
            {
                if (!string.IsNullOrEmpty(to))
                    message.To.Add(new MailAddress(to));
            }
            if (message.To.Count > 0)
            {

                message.Subject = subject;

                var viewContent = await ViewRender.RenderToStringAsync(viewName, model);

                message.IsBodyHtml = true;
                message.Body = viewContent;
                smtp.Port = EmailSettings.Port;
                smtp.Host = EmailSettings.Host;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                try
                {
#if !DEBUG
                    await smtp.SendMailAsync(message);
#endif
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogError("Erro no disparo de email: {Error}.", e.Message);
                    Logger.LogError("StackError: {Error}", e.StackTrace);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> Send<T>(string toEmail, string subject, string viewName, T model) where T : class
        {
            return await Send(new[] { toEmail }, subject, viewName, model);
        }
    }
}
