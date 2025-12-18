using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;
using WebAppServer.Services;

namespace WebAppServer.Pages.Courses
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly ICourseReportService _courseReportService;

        public EditModel(AppDbContext db, ICourseReportService courseReportService)
        {
            _db = db;
            _courseReportService = courseReportService;
        }

        [BindProperty]
        public Course Course { get; set; } = new();

        [BindProperty]
        public string NewUserLogin { get; set; } = string.Empty;

        [BindProperty]
        public string NewTaskName { get; set; } = string.Empty;

        [BindProperty]
        public string NewTaskDescription { get; set; } = string.Empty;

        public async System.Threading.Tasks.Task<IActionResult> OnGetAsync(int id)
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
        public async System.Threading.Tasks.Task<IActionResult> OnPostUpdateAsync()
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
        public async System.Threading.Tasks.Task<IActionResult> OnPostAddUserAsync()
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
        public async System.Threading.Tasks.Task<IActionResult> OnPostAddTaskAsync()
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
        public async System.Threading.Tasks.Task<IActionResult> OnPostDeleteCourseAsync()
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

        public async System.Threading.Tasks.Task<IActionResult> OnPostDownloadReportAsync(int id, string format)
        {
            var course = await _db.Courses
                .Include(c => c.Avtors)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            var login = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(login))
            {
                return Unauthorized();
            }

            if (!course.Avtors.Any(a => a.Login == login))
            {
                return Forbid();
            }

            var requestedFormat = string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase)
                ? CourseReportFormat.Excel
                : CourseReportFormat.Pdf;

            var report = await _courseReportService.GenerateCourseReportAsync(id, login, requestedFormat);
            if (report.Status == CourseReportGenerationStatus.Forbidden)
            {
                return Forbid();
            }

            if (report.Status == CourseReportGenerationStatus.NotFound || report.File == null)
            {
                return NotFound();
            }

            return File(report.File.Content, report.File.ContentType, report.File.FileName);
        }
    }
}
