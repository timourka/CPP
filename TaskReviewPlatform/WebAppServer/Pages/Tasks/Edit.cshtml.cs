using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;
using System.Linq;
using WebAppServer.Services;

namespace WebAppServer.Pages.Tasks
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notificationService;
        public EditModel(AppDbContext db, INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        [BindProperty]
        public Models.Models.Task Task { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? StudentFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public List<Answer> Answers { get; set; } = new();
        public Dictionary<int, int> ReviewCommentsCount { get; set; } = new();
        public List<string> StudentOptions { get; set; } = new();
        public List<string> StatusOptions { get; set; } = new();

        public async System.Threading.Tasks.Task<IActionResult> OnGetAsync(int id)
        {
            Task = await _db.Tasks
                .Include(t => t.Course)
                .Include(t => t.Course!.Avtors)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Task == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!Task.Course!.Avtors.Any(a => a.Login == login))
                return Forbid();

            SearchQuery = SearchQuery?.Trim() ?? string.Empty;
            StudentFilter = string.IsNullOrWhiteSpace(StudentFilter) ? null : StudentFilter.Trim();
            StatusFilter = string.IsNullOrWhiteSpace(StatusFilter) ? null : StatusFilter.Trim();

            var answersQuery = _db.Answers
                .Include(a => a.Student)
                .Include(a => a.Files)
                .Where(a => a.Task!.Id == id);

            StudentOptions = await answersQuery
                .Where(a => a.Student != null)
                .Select(a => a.Student!.Login)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            StatusOptions = await answersQuery
                .Select(a => a.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(StudentFilter))
            {
                answersQuery = answersQuery.Where(a => a.Student != null && a.Student.Login == StudentFilter);
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                answersQuery = answersQuery.Where(a => a.Status == StatusFilter);
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var pattern = $"%{SearchQuery}%";
                answersQuery = answersQuery.Where(a =>
                    EF.Functions.Like(a.Text ?? string.Empty, pattern) ||
                    EF.Functions.Like(a.Student!.Login ?? string.Empty, pattern));
            }

            Answers = await answersQuery
                .OrderBy(a => a.Student != null ? a.Student.Login : string.Empty)
                .ThenBy(a => a.Id)
                .ToListAsync();

            var ids = Answers.Select(a => a.Id).ToList();
            ReviewCommentsCount = await _db.ReviewComments
                .Where(c => ids.Contains(c.Answer!.Id))
                .GroupBy(c => c.Answer!.Id)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            return Page();
        }

        public async System.Threading.Tasks.Task<IActionResult> OnPostUpdateAsync()
        {
            var t = await _db.Tasks.Include(x => x.Course).ThenInclude(c => c.Avtors)
                .FirstOrDefaultAsync(x => x.Id == Task.Id);

            if (t == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!t.Course!.Avtors.Any(a => a.Login == login))
                return Forbid();

            t.Name = Task.Name;
            t.Description = Task.Description;

            await _db.SaveChangesAsync();
            return RedirectToPage("/Tasks/Edit", new { id = t.Id });
        }

        public async System.Threading.Tasks.Task<IActionResult> OnPostDeleteAsync()
        {
            var t = await _db.Tasks.Include(x => x.Course).ThenInclude(c => c.Avtors)
                .FirstOrDefaultAsync(x => x.Id == Task.Id);

            if (t == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!t.Course!.Avtors.Any(a => a.Login == login))
                return Forbid();

            _db.Tasks.Remove(t);
            await _db.SaveChangesAsync();
            return RedirectToPage("/Courses/Edit", new { id = t.Course!.Id });
        }

        // Простейшая проверка ответов
        public async System.Threading.Tasks.Task<IActionResult> OnPostCheckAnswerAsync(int answerId)
        {
            var answer = await _db.Answers.Include(a => a.Task).ThenInclude(t => t.Course).ThenInclude(c => c.Avtors)
                .FirstOrDefaultAsync(a => a.Id == answerId);

            if (answer == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!answer.Task!.Course!.Avtors.Any(a => a.Login == login))
                return Forbid();

            answer.Grade = 100; // например, проверка + выставление балла
            answer.Status = "Проверено";
            answer.ReviewRequested = false;
            answer.AllowResubmit = false;

            var requests = await _db.ReviewRequests
                .Where(r => r.Answer!.Id == answerId)
                .ToListAsync();
            foreach (var r in requests)
            {
                r.Completed = true;
            }
            await _db.SaveChangesAsync();
            await _notificationService.NotifyStatusChangeAsync(answer, answer.Status);

            return RedirectToPage("/Tasks/Edit", new { id = answer.Task.Id });
        }

        public async System.Threading.Tasks.Task<IActionResult> OnPostAllowRetryAsync(int answerId)
        {
            var answer = await _db.Answers.Include(a => a.Task).ThenInclude(t => t.Course).ThenInclude(c => c.Avtors)
                .FirstOrDefaultAsync(a => a.Id == answerId);

            if (answer == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!answer.Task!.Course!.Avtors.Any(a => a.Login == login))
                return Forbid();

            answer.AllowResubmit = true;
            answer.Status = "Разрешена повторная отправка";
            answer.ReviewRequested = false;
            answer.Grade = -1;

            var requests = await _db.ReviewRequests
                .Where(r => r.Answer!.Id == answerId)
                .ToListAsync();
            foreach (var r in requests)
            {
                r.Completed = false;
            }
            await _db.SaveChangesAsync();
            await _notificationService.NotifyStatusChangeAsync(answer, answer.Status);

            return RedirectToPage("/Tasks/Edit", new { id = answer.Task.Id });
        }
    }
}
