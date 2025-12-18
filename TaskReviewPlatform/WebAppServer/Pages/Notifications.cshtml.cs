using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;
using System.Linq;

namespace WebAppServer.Pages
{
    [Authorize]
    public class NotificationsModel : PageModel
    {
        private readonly AppDbContext _db;

        public NotificationsModel(AppDbContext db)
        {
            _db = db;
        }

        public List<Notification> Items { get; set; } = new();

        public async System.Threading.Tasks.Task<IActionResult> OnGetAsync()
        {
            var login = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(login))
            {
                return RedirectToPage("/Login");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login);
            if (user == null)
            {
                return Unauthorized();
            }

            Items = await _db.Notifications
                .Include(n => n.Answer)!.ThenInclude(a => a!.Task)!.ThenInclude(t => t!.Course)
                .Where(n => n.User!.Id == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

            var unread = Items.Where(n => !n.IsRead).ToList();
            if (unread.Count > 0)
            {
                foreach (var n in unread)
                {
                    n.IsRead = true;
                }
                await _db.SaveChangesAsync();
            }

            return Page();
        }
    }
}
