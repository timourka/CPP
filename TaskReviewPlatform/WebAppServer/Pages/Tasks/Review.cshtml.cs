using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;
using Microsoft.AspNetCore.Hosting;

namespace WebAppServer.Pages.Tasks
{
    [Authorize]
    public class ReviewModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ReviewModel(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public Answer? Answer { get; set; }

        public List<ReviewComment> Comments { get; set; } = new();

        public List<ReviewFile> Files { get; set; } = new();

        [BindProperty]
        public string? ActiveFilePath { get; set; }

        public string? ActiveFileName { get; set; }

        public string ActiveFileContent { get; set; } = string.Empty;

        public string ActiveFileLanguage { get; set; } = "plaintext";

        public List<string> ActiveFileLines { get; set; } = new();

        public IReadOnlyList<string> MonacoSupportedExtensions { get; } = MonacoSupport.MonacoSupportedExtensions;

        public string? FileError { get; set; }

        [BindProperty]
        public int Grade { get; set; }

        [BindProperty]
        public bool AllowResubmit { get; set; }

        [BindProperty]
        public int? LineNumber { get; set; }

        [BindProperty]
        public string CommentText { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int answerId, string? file)
        {
            var loadResult = await LoadAnswerAsync(answerId);
            if (loadResult != null)
            {
                return loadResult;
            }

            Grade = Answer!.Grade >= 0 ? Answer.Grade : 0;
            AllowResubmit = Answer.AllowResubmit;

            BuildFiles();
            ActiveFilePath = string.IsNullOrWhiteSpace(file) ? Files.FirstOrDefault()?.RelativePath : file;

            if (!string.IsNullOrWhiteSpace(ActiveFilePath))
            {
                LoadFileContent(ActiveFilePath);
            }

            await LoadCommentsAsync(answerId, ActiveFileName);
            return Page();
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int answerId)
        {
            var loadResult = await LoadAnswerAsync(answerId);
            if (loadResult != null)
            {
                return loadResult;
            }

            BuildFiles();

            if (!string.IsNullOrWhiteSpace(ActiveFilePath))
            {
                LoadFileContent(ActiveFilePath);
            }

            if (Files.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "У ответа нет файлов для комментирования.");
                await LoadCommentsAsync(answerId, ActiveFileName);
                return Page();
            }

            if (string.IsNullOrWhiteSpace(ActiveFilePath) || Files.All(f => f.RelativePath != ActiveFilePath))
            {
                ModelState.AddModelError(string.Empty, "Выберите файл для комментария.");
                await LoadCommentsAsync(answerId, ActiveFileName);
                return Page();
            }

            if (!string.IsNullOrEmpty(FileError))
            {
                ModelState.AddModelError(string.Empty, FileError);
                await LoadCommentsAsync(answerId, ActiveFileName);
                return Page();
            }

            if (string.IsNullOrWhiteSpace(CommentText))
            {
                ModelState.AddModelError(string.Empty, "Комментарий не может быть пустым");
                await LoadCommentsAsync(answerId, ActiveFileName);
                return Page();
            }

            if (!LineNumber.HasValue || LineNumber <= 0)
            {
                ModelState.AddModelError(string.Empty, "Нужно выбрать строку в файле для комментария.");
                await LoadCommentsAsync(answerId, ActiveFileName);
                return Page();
            }

            var reviewer = await _db.Users.FirstOrDefaultAsync(u => u.Login == User.Identity!.Name);

            var selectedFile = Files.FirstOrDefault(f => f.RelativePath == ActiveFilePath);
            var comment = new ReviewComment
            {
                Answer = Answer,
                Reviewer = reviewer,
                Text = CommentText.Trim(),
                LineNumber = LineNumber,
                FileName = selectedFile?.Name,
                CreatedAt = DateTime.UtcNow
            };

            _db.ReviewComments.Add(comment);
            await _db.SaveChangesAsync();

            return RedirectToPage(new { answerId, file = ActiveFilePath });
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
                .Include(a => a.Files)
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

        private async System.Threading.Tasks.Task LoadCommentsAsync(int answerId, string? fileName)
        {
            var query = _db.ReviewComments
                .Include(c => c.Reviewer)
                .Where(c => c.Answer!.Id == answerId);

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                query = query.Where(c => c.FileName == fileName || c.FileName == null);
            }

            Comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        private void BuildFiles()
        {
            Files.Clear();

            if (Answer?.Files?.Count > 0)
            {
                foreach (var file in Answer.Files)
                {
                    Files.Add(new ReviewFile
                    {
                        Name = string.IsNullOrWhiteSpace(file.FileName) ? "Файл ответа" : file.FileName,
                        RelativePath = file.RelativePath
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(Answer?.FilePath))
            {
                Files.Add(new ReviewFile
                {
                    Name = string.IsNullOrWhiteSpace(Answer.FileName) ? "Файл ответа" : Answer.FileName!,
                    RelativePath = Answer.FilePath!
                });
            }
        }

        private void LoadFileContent(string relativePath)
        {
            var file = Files.FirstOrDefault(f => f.RelativePath == relativePath);
            if (file == null)
            {
                FileError = "Файл не найден";
                return;
            }

            ActiveFileName = file.Name;
            ActiveFileLanguage = MonacoSupport.MonacoLanguageByExtension.TryGetValue(Path.GetExtension(file.Name), out var language)
                ? language
                : "plaintext";

            if (!MonacoSupport.AllowedPreviewExtensions.Contains(Path.GetExtension(file.Name)))
            {
                FileError = "Просмотр поддерживается только для текстовых файлов.";
                return;
            }

            var localPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, localPath);

            if (!System.IO.File.Exists(fullPath))
            {
                FileError = "Файл недоступен на сервере.";
                return;
            }

            try
            {
                ActiveFileLines = System.IO.File.ReadAllLines(fullPath).ToList();
                ActiveFileContent = string.Join("\n", ActiveFileLines);
            }
            catch (Exception)
            {
                FileError = "Не удалось прочитать файл.";
            }
        }

        public class ReviewFile
        {
            public string Name { get; set; } = string.Empty;

            public string RelativePath { get; set; } = string.Empty;
        }
    }
}
