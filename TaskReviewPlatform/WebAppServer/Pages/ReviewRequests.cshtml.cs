using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;

namespace WebAppServer.Pages
{
    [Authorize]
    public class ReviewRequestsModel : PageModel
    {
        private readonly AppDbContext _db;

        public ReviewRequestsModel(AppDbContext db)
        {
            _db = db;
        }

        public List<ReviewRequest> Requests { get; set; } = new();

        public async Task OnGetAsync()
        {
            var login = User.Identity!.Name;

            Requests = await _db.ReviewRequests
                .Include(r => r.Reviewer)
                .Include(r => r.Answer)!
                    .ThenInclude(a => a!.Task)!
                        .ThenInclude(t => t!.Course)
                .Include(r => r.Answer)!
                    .ThenInclude(a => a!.Student)
                .Where(r => r.Reviewer!.Login == login)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
