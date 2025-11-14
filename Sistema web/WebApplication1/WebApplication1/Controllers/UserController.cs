using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Construtor do controller recebe o contexto do Entity Framework
        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= LOGIN =================
        // GET: exibe a página de login
        public IActionResult Login()
        {
            return View();
        }

       
        [HttpPost]
        public async Task<JsonResult> LoginAjax(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "Preencha todos os campos." });
            }

            var usuario = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.Password == password);

            if (usuario == null)
            {
                return Json(new { success = false, message = "Email ou senha inválidos!" });
            }

            // Salva na sessão
            HttpContext.Session.SetString("Username", usuario.Username);
            HttpContext.Session.SetString("Email", usuario.Email);

            // Monta a URL de redirecionamento
            string redirectUrl = Url.Action("Novo", "Tickets") ?? "/Tickets/Novo";

            // ✅ Retorna também o username no JSON
            return Json(new
            {
                success = true,
                message = $"Bem-vindo(a), {usuario.Username}!",
                username = usuario.Username,   // <-- este campo o Android usa
                redirectUrl = redirectUrl
            });
        }


        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            // Limpa todas as variáveis da sessão
            HttpContext.Session.Clear();

            // Redireciona para a tela de login
            return RedirectToAction("Login");
        }
    }
}
