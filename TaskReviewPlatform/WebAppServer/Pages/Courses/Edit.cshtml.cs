using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;

namespace WebAppServer.Pages.Courses
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;

        public EditModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public Course Course { get; set; } = new();

        [BindProperty]
        public string NewUserLogin { get; set; } = string.Empty;

        [BindProperty]
        public string NewTaskName { get; set; } = string.Empty;

        [BindProperty]
        public string NewTaskDescription { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Course = await _db.Courses
                .Include(c => c.Avtors)
                .Include(c => c.Participants)
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (Course == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!Course.Avtors.Any(a => a.Login == login))
                return Forbid(); // только авторы

            return Page();
        }

        // Обновление курса
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var course = await _db.Courses.FindAsync(Course.Id);
            if (course == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!_db.Courses.Include(c => c.Avtors).First(c => c.Id == course.Id).Avtors.Any(a => a.Login == login))
                return Forbid();

            course.Name = Course.Name;
            course.Description = Course.Description;

            await _db.SaveChangesAsync();
            return RedirectToPage("/Courses/Edit", new { id = course.Id });
        }

        // Добавить участника по логину
        public async Task<IActionResult> OnPostAddUserAsync()
        {
            var course = await _db.Courses
                .Include(c => c.Participants)
                .Include(c => c.Avtors)
                .FirstOrDefaultAsync(c => c.Id == Course.Id);

            if (course == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!course.Avtors.Any(a => a.Login == login))
                return Forbid();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == NewUserLogin);
            if (user != null && !course.Participants.Contains(user))
            {
                course.Participants.Add(user);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage("/Courses/Edit", new { id = course.Id });
        }

        // Создать задание
        public async Task<IActionResult> OnPostAddTaskAsync()
        {
            var course = await _db.Courses
                .Include(c => c.Tasks)
                .Include(c => c.Avtors)
                .FirstOrDefaultAsync(c => c.Id == Course.Id);

            if (course == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!course.Avtors.Any(a => a.Login == login))
                return Forbid();

            var task = new Models.Models.Task
            {
                Name = NewTaskName,
                Description = NewTaskDescription,
                Course = course
            };
            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Courses/Edit", new { id = course.Id });
        }

        // Удалить курс
        public async Task<IActionResult> OnPostDeleteCourseAsync()
        {
            var course = await _db.Courses
                .Include(c => c.Avtors)
                .FirstOrDefaultAsync(c => c.Id == Course.Id);

            if (course == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!course.Avtors.Any(a => a.Login == login))
                return Forbid();

            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();
            return RedirectToPage("/Courses");
        }
    }
}
