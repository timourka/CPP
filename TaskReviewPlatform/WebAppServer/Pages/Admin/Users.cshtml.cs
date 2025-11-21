using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Repository.Data;

namespace WebAppServer.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly AppDbContext _db;

        public UsersModel(AppDbContext db)
        {
            _db = db;
        }

        // Список пользователей
        public List<User> Users { get; set; } = new();

        // Для создания/редактирования
        [BindProperty]
        public User InputUser { get; set; } = new();

        // Для редактирования
        [BindProperty(SupportsGet = true)]
        public int? EditId { get; set; }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            Users = await _db.Users.ToListAsync();

            if (EditId.HasValue)
            {
                var user = await _db.Users.FindAsync(EditId.Value);
                if (user != null)
                    InputUser = user;
            }
        }

        // Создание или обновление пользователя
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (InputUser.Id == 0)
            {
                // Создание
                _db.Users.Add(InputUser);
            }
            else
            {
                // Обновление
                var user = await _db.Users.FindAsync(InputUser.Id);
                if (user == null)
                    return NotFound();

                user.Login = InputUser.Login;
                user.Name = InputUser.Name;
                user.Password = InputUser.Password;
            }

            await _db.SaveChangesAsync();
            return RedirectToPage("/Admin/Users");
        }

        // Удаление пользователя
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage("/Admin/Users");
        }
    }
}
