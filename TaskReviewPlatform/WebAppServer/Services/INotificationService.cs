using Models.Models;

namespace WebAppServer.Services
{
    public interface INotificationService
    {
        System.Threading.Tasks.Task NotifyStatusChangeAsync(Answer answer, string statusMessage);

        System.Threading.Tasks.Task NotifyReviewCommentAsync(Answer answer, ReviewComment comment);

        System.Threading.Tasks.Task NotifyReviewerAssignmentAsync(ReviewRequest request);
    }
}
