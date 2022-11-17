using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PeD.Core;
using PeD.Views.Email;

namespace PeD.Services
{
    public class SendGridService
    {
        protected EmailConfig EmailConfig;
        protected MailService MailService;
        protected IViewRenderService ViewRender;
        protected ILogger<SendGridService> Logger;
        
        public SendGridService(IViewRenderService viewRender, MailService mailService, EmailConfig emailConfig, ILogger<SendGridService> logger)
        {
            EmailConfig = emailConfig;
            MailService = mailService;
            Logger = logger;
        }

        public async Task<bool> Send(string to, string subject, string content, string title = null,
            string actionLabel = null, string actionUrl = null)
        {
            return await Send(new[] {to}, subject, content, title, actionLabel, actionUrl);
        }

        public async Task<bool> Send(string[] tos, string subject, string content, string title = null,
            string actionLabel = null, string actionUrl = null)
        {
            title ??= subject;
            return await Send(tos, subject, "Email/SimpleMail",
                new SimpleMail()
                    {Titulo = title, Conteudo = content, ActionLabel = actionLabel, ActionUrl = actionUrl});
        }

        public async Task<bool> Send<T>(string[] tos, string subject, string viewName, T model) where T : class
        {
            var res = await MailService.Send(tos, subject, viewName, model);
            return res;
        }

        public async Task<bool> Send<T>(string to, string subject, string viewName, T model) where T : class
        {
            var res = await MailService.Send(to, subject, viewName, model);
            return res;
        }
    }
}