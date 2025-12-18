using Models.Models;

namespace WebAppServer.Services
{
    public interface IEmailSender
    {
        System.Threading.Tasks.Task SendEmailAsync(User user, string subject, string body);
    }
}
