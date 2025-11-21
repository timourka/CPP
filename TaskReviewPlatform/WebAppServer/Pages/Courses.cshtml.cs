using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;

namespace WebAppServer.Pages
{
    public class CoursesModel : PageModel
    {
        private readonly AppDbContext _db;

        public CoursesModel(AppDbContext db)
        {
            _db = db;
        }

        public List<Course> AuthorCourses { get; set; } = new();
        public List<Course> ParticipantCourses { get; set; } = new();

        [BindProperty]
        public string NewCourseName { get; set; } = string.Empty;

        [BindProperty]
        public string NewCourseDescription { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToPage("/Login");

            var login = User.Identity.Name;

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Login == login);

            if (user == null)
                return Unauthorized();

            // получаем списки курсов по ролям
            AuthorCourses = await _db.Courses
                .Where(c => c.Avtors.Any(a => a.Id == user.Id))
                .ToListAsync();

            ParticipantCourses = await _db.Courses
                .Where(c => c.Participants.Any(p => p.Id == user.Id))
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToPage("/Login");

            if (string.IsNullOrWhiteSpace(NewCourseName))
            {
                ModelState.AddModelError("", "Название курса обязательно.");
                return await OnGetAsync();
            }

            var login = User.Identity.Name;

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Login == login);

            if (user == null)
                return Unauthorized();

            var course = new Course
            {
                Name = NewCourseName.Trim(),
                Description = NewCourseDescription.Trim(),
                Avtors = new List<User> { user }
            };

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Courses");
        }
    }
}
