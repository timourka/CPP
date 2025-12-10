using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _env;

        public AnswersModel(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public Models.Models.Task? Task { get; set; }
        public List<Answer> UserAnswers { get; set; } = new();
        public Dictionary<int, List<ReviewComment>> ReviewComments { get; set; } = new();

        [BindProperty]
        public string NewAnswerText { get; set; } = string.Empty;

        [BindProperty]
        public List<IFormFile> NewAnswerFiles { get; set; } = new();

        public IReadOnlyList<string> MonacoSupportedExtensions => MonacoSupport.MonacoSupportedExtensions;

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
                    .Include(a => a.Files)
                    .Where(a => a.Task!.Id == id && a.Student!.Id == user.Id)
                    .ToListAsync();

                var answerIds = UserAnswers.Select(a => a.Id).ToList();
                ReviewComments = await _db.ReviewComments
                    .Include(c => c.Reviewer)
                    .Where(c => answerIds.Contains(c.Answer!.Id))
                    .GroupBy(c => c.Answer!.Id)
                    .ToDictionaryAsync(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).ToList());
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

            var files = await SaveFilesAsync(NewAnswerFiles);

            var answer = new Answer
            {
                Text = NewAnswerText.Trim(),
                Student = user,
                Task = Task,
                FilePath = files.FirstOrDefault()?.RelativePath,
                FileName = files.FirstOrDefault()?.FileName,
                Files = files,
                Grade = -1,
                Status = "Черновик",
                ReviewRequested = false,
                AllowResubmit = false
            };

            foreach (var file in files)
            {
                file.Answer = answer;
            }

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

            foreach (var f in answer.Files)
            {
                DeleteFileIfExists(f.RelativePath);
            }
            DeleteFileIfExists(answer.FilePath);
            _db.AnswerFiles.RemoveRange(answer.Files);
            _db.Answers.Remove(answer);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Tasks/Answers", new { id = answer.Task!.Id });
        }

        public async Task<IActionResult> OnPostEditAsync(int answerId, string newText, List<IFormFile>? newFiles)
        {
            var answer = await _db.Answers
                .Include(a => a.Student)
                .Include(a => a.Task)
                .Include(a => a.Files)
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

            if (newFiles != null && newFiles.Any(f => f.Length > 0))
            {
                _db.AnswerFiles.RemoveRange(answer.Files);

                foreach (var f in answer.Files)
                {
                    DeleteFileIfExists(f.RelativePath);
                }

                answer.Files.Clear();

                var savedFiles = await SaveFilesAsync(newFiles.Where(f => f.Length > 0));
                answer.Files.AddRange(savedFiles);

                foreach (var file in savedFiles)
                {
                    file.Answer = answer;
                }

                var firstFile = savedFiles.FirstOrDefault();
                answer.FilePath = firstFile?.RelativePath;
                answer.FileName = firstFile?.FileName;
            }
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

        private async Task<List<AnswerFile>> SaveFilesAsync(IEnumerable<IFormFile> files)
        {
            var saved = new List<AnswerFile>();

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    continue;
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using var stream = System.IO.File.Create(filePath);
                await file.CopyToAsync(stream);

                saved.Add(new AnswerFile
                {
                    FileName = file.FileName,
                    RelativePath = $"/uploads/{fileName}"
                });
            }

            return saved;
        }

        private void DeleteFileIfExists(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var localPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, localPath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
