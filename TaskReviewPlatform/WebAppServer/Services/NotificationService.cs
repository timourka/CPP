using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models.Models;
using Repository.Data;
using System;
using System.Linq;
using System.Text;

namespace WebAppServer.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext db, IEmailSender emailSender, ILogger<NotificationService> logger)
        {
            _db = db;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task NotifyStatusChangeAsync(Answer answer, string statusMessage)
        {
            await EnsureAnswerRelationsAsync(answer);

            var student = answer.Student;
            if (student == null)
            {
                _logger.LogWarning("Cannot send status notification: answer {AnswerId} has no student", answer.Id);
                return;
            }

            var title = "Изменение статуса ответа";
            var message = $"Статус ответа по задаче \"{answer.Task?.Name}\" обновлён: {statusMessage}.";

            await CreateNotificationAsync(student, answer, title, message);
        }

        public async System.Threading.Tasks.Task NotifyReviewCommentAsync(Answer answer, ReviewComment comment)
        {
            await EnsureAnswerRelationsAsync(answer);

            var student = answer.Student;
            if (student == null)
            {
                _logger.LogWarning("Cannot send comment notification: answer {AnswerId} has no student", answer.Id);
                return;
            }

            var reviewerName = comment.Reviewer?.Login ?? "рецензент";
            var title = "Новый комментарий к ответу";

            var preview = Truncate(comment.Text, 150);
            var message = $"Новый комментарий от {reviewerName} по задаче \"{answer.Task?.Name}\": {preview}";

            await CreateNotificationAsync(student, answer, title, message);
        }

        public async System.Threading.Tasks.Task NotifyReviewerAssignmentAsync(ReviewRequest request)
        {
            if (request.Reviewer == null)
            {
                _logger.LogWarning("Cannot notify reviewer: request {RequestId} has no reviewer", request.Id);
                return;
            }

            if (request.Answer == null)
            {
                _logger.LogWarning("Cannot notify reviewer: request {RequestId} has no answer", request.Id);
                return;
            }

            await EnsureAnswerRelationsAsync(request.Answer);

            var studentName = request.Answer.Student?.Login ?? "студент";
            var title = "Новый запрос на рецензию";
            var message = $"Вам назначена рецензия ответа от {studentName} по задаче \"{request.Answer.Task?.Name}\".";

            await CreateNotificationAsync(request.Reviewer, request.Answer, title, message);
        }

        private async System.Threading.Tasks.Task CreateNotificationAsync(User user, Answer? answer, string title, string message)
        {
            var notification = new Notification
            {
                User = user,
                Answer = answer,
                Title = title,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            await _emailSender.SendEmailAsync(user, title, message);
        }

        private async System.Threading.Tasks.Task EnsureAnswerRelationsAsync(Answer answer)
        {
            if (answer.Student == null)
            {
                await _db.Entry(answer).Reference(a => a.Student).LoadAsync();
            }

            if (answer.Task == null)
            {
                await _db.Entry(answer).Reference(a => a.Task).LoadAsync();
            }

            if (answer.Task != null && answer.Task.Course == null)
            {
                await _db.Entry(answer.Task).Reference(t => t.Course).LoadAsync();
            }
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
            {
                return text;
            }

            var builder = new StringBuilder();
            builder.Append(text.AsSpan(0, maxLength));
            builder.Append("...");
            return builder.ToString();
        }
    }
}
