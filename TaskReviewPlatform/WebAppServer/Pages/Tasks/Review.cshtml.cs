using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;

namespace WebAppServer.Pages.Tasks
{
    [Authorize]
    public class ReviewModel : PageModel
    {
        private readonly AppDbContext _db;

        public ReviewModel(AppDbContext db)
        {
            _db = db;
        }

        public Answer? Answer { get; set; }

        public List<ReviewComment> Comments { get; set; } = new();

        [BindProperty]
        public int Grade { get; set; }

        [BindProperty]
        public bool AllowResubmit { get; set; }

        [BindProperty]
        public int? LineNumber { get; set; }

        [BindProperty]
        public string CommentText { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int answerId)
        {
            var loadResult = await LoadAnswerAsync(answerId);
            if (loadResult != null)
            {
                return loadResult;
            }

            Grade = Answer!.Grade >= 0 ? Answer.Grade : 0;
            AllowResubmit = Answer.AllowResubmit;

            await LoadCommentsAsync(answerId);
            return Page();
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int answerId)
        {
            var loadResult = await LoadAnswerAsync(answerId);
            if (loadResult != null)
            {
                return loadResult;
            }

            if (string.IsNullOrWhiteSpace(CommentText))
            {
                ModelState.AddModelError(string.Empty, "Комментарий не может быть пустым");
                await LoadCommentsAsync(answerId);
                return Page();
            }

            var reviewer = await _db.Users.FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);
            var comment = new ReviewComment
            {
                Answer = Answer,
                Reviewer = reviewer,
                Text = CommentText.Trim(),
                LineNumber = LineNumber,
                FileName = Answer!.FileName,
                CreatedAt = DateTime.UtcNow
            };

            _db.ReviewComments.Add(comment);
            await _db.SaveChangesAsync();

            return RedirectToPage(new { answerId });
        }

        public async Task<IActionResult> OnPostFinalizeAsync(int answerId)
        {
            var loadResult = await LoadAnswerAsync(answerId);
            if (loadResult != null)
            {
                return loadResult;
            }

            Answer!.Grade = Grade;
            Answer.Status = "Проверено";
            Answer.ReviewRequested = false;
            Answer.AllowResubmit = AllowResubmit;

            await _db.SaveChangesAsync();
            return RedirectToPage(new { answerId });
        }

        private async Task<IActionResult?> LoadAnswerAsync(int answerId)
        {
            Answer = await _db.Answers
                .Include(a => a.Student)
                .Include(a => a.Task)!.ThenInclude(t => t.Course)!.ThenInclude(c => c.Avtors)
                .FirstOrDefaultAsync(a => a.Id == answerId);

            if (Answer == null)
            {
                return NotFound();
            }

            var login = User.Identity!.Name;
            if (!Answer.Task!.Course!.Avtors.Any(a => a.Login == login))
            {
                return Forbid();
            }

            return null;
        }

        private async Task LoadCommentsAsync(int answerId)
        {
            Comments = await _db.ReviewComments
                .Include(c => c.Reviewer)
                .Where(c => c.Answer!.Id == answerId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
