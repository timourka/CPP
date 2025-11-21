using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Repository;
using Models.Models;

namespace WebAppServer.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IRepository<User> _users;

        public LoginModel(IRepository<User> users)
        {
            _users = users;
        }

        [BindProperty]
        public string Login { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            // 🔥 1. Проверяем хардкоженного админа
            if (Login == "admin" && Password == "admin")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.Role, "Admin") // <= ВАЖНО
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                return RedirectToPage("/Admin/Panel"); // куда угодно
            }

            // 🔥 2. Обычный пользователь (из БД)
            var allUsers = await _users.GetAll();
            var user = allUsers.FirstOrDefault(u =>
                u.Login == Login && u.Password == Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Неверный логин или пароль");
                return Page();
            }

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Login),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, "User")       // <= обычный пользователь
            };

            var userIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(userIdentity));

            return RedirectToPage("/Index");
        }
    }
}
