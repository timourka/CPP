using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;

namespace WebAppServer.Pages.Tasks
{
    [Authorize]
    public class AnswersModel : PageModel
    {
        private readonly AppDbContext _db;

        public AnswersModel(AppDbContext db)
        {
            _db = db;
        }

        public Models.Models.Task? Task { get; set; }
        public List<Answer> UserAnswers { get; set; } = new();

        [BindProperty]
        public string NewAnswerText { get; set; } = string.Empty;

        private bool CanModifyAnswer(Answer answer, string login)
        {
            return answer.Student!.Login == login &&
                   (answer.Status != "Проверено" || answer.AllowResubmit);
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Task = await _db.Tasks
                .Include(t => t.Course)
                .ThenInclude(c => c.Participants)
                .Include(t => t.Course)
                .ThenInclude(c => c.Avtors)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Task == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!Task.Course!.Participants.Any(p => p.Login == login) &&
                !Task.Course.Avtors.Any(a => a.Login == login)) // автор тоже может
            {
                return Forbid();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (user != null)
            {
                UserAnswers = await _db.Answers
                    .Include(a => a.Student)
                    .Where(a => a.Task!.Id == id && a.Student!.Id == user.Id)
                    .ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(int id)
        {
            Task = await _db.Tasks
                .Include(t => t.Course)
                .Include(t => t.Course.Avtors)
                .Include(t => t.Course.Participants)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Task == null)
                return NotFound();

            var login = User.Identity!.Name;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (user == null)
                return Forbid();

            var answer = new Answer
            {
                Text = NewAnswerText.Trim(),
                Student = user,
                Task = Task,
                Grade = -1,
                Status = "Черновик",
                ReviewRequested = false,
                AllowResubmit = false
            };

            _db.Answers.Add(answer);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Tasks/Answers", new { id = id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int answerId)
        {
            var answer = await _db.Answers
                .Include(a => a.Student)
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == answerId);

            if (answer == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!CanModifyAnswer(answer, login))
                return Forbid();

            _db.Answers.Remove(answer);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Tasks/Answers", new { id = answer.Task!.Id });
        }

        public async Task<IActionResult> OnPostEditAsync(int answerId, string newText)
        {
            var answer = await _db.Answers
                .Include(a => a.Student)
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == answerId);

            if (answer == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!CanModifyAnswer(answer, login))
                return Forbid();

            answer.Text = newText.Trim();
            answer.Status = "Черновик";
            answer.ReviewRequested = false;
            answer.Grade = -1;
            await _db.SaveChangesAsync();

            return RedirectToPage("/Tasks/Answers", new { id = answer.Task!.Id });
        }

        public async Task<IActionResult> OnPostRequestReviewAsync(int answerId)
        {
            var answer = await _db.Answers
                .Include(a => a.Student)
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == answerId);

            if (answer == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!CanModifyAnswer(answer, login))
                return Forbid();

            answer.Status = "Ожидает проверки";
            answer.ReviewRequested = true;
            answer.AllowResubmit = false;
            answer.Grade = -1;

            await _db.SaveChangesAsync();

            return RedirectToPage("/Tasks/Answers", new { id = answer.Task!.Id });
        }
    }
}
