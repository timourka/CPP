using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;

namespace WebAppServer.Pages.Courses
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _db;

        public DetailsModel(AppDbContext db)
        {
            _db = db;
        }

        public Course? Course { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Course = await _db.Courses
                .Include(c => c.Tasks)
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (Course == null)
                return NotFound();

            var login = User.Identity!.Name;
            if (!Course.Participants.Any(p => p.Login == login) &&
                !Course.Avtors.Any(a => a.Login == login)) // автор тоже может смотреть
            {
                return Forbid();
            }

            return Page();
        }
    }
}
