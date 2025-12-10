using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;

namespace WebAppServer.Pages.Tasks
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;
        public EditModel(AppDbContext db) => _db = db;

        [BindProperty]
        public Models.Models.Task Task { get; set; } = new();

        public List<Answer> Answers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
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

            Answers = await _db.Answers
                .Include(a => a.Student)
                .Where(a => a.Task!.Id == id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
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

        public async Task<IActionResult> OnPostDeleteAsync()
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
        public async Task<IActionResult> OnPostCheckAnswerAsync(int answerId)
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
            await _db.SaveChangesAsync();

            return RedirectToPage("/Tasks/Edit", new { id = answer.Task.Id });
        }
    }
}
