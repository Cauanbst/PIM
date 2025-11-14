using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;


namespace WebApplication1.Controllers
{
    public class TecnicoController : Controller
    {
        // Contexto do banco de dados (Entity Framework)
        private readonly ApplicationDbContext _context;

        // Construtor do controller que recebe o contexto por injeção de dependência
        public TecnicoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= LOGIN =================
        // GET: Exibe a página de login do técnico
        [HttpGet]
        public IActionResult Login()
        {
            // Apenas retorna a view de login
            return View();
        }

        // POST: Processa login via AJAX
        [HttpPost]
        public async Task<JsonResult> LoginAjax(string email, string password)
        {
            // Validação básica: campos não podem estar vazios
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                // Retorna JSON informando que os campos são obrigatórios
                return Json(new { success = false, message = "Email e senha são obrigatórios!" });
            }

            // Consulta no banco de dados se existe técnico com email e senha informados
            var tecnico = await _context.TbTecnico
                .FirstOrDefaultAsync(t => t.Email.ToLower() == email.ToLower() && t.Senha == password);

            // Se não encontrar o técnico
            if (tecnico == null)
            {
                // Retorna JSON com sucesso = false e mensagem de erro
                return Json(new { success = false, message = "Email ou senha inválidos!" });
            }

            // Se encontrou o técnico, salva informações na sessão
            HttpContext.Session.SetString("Username", tecnico.Nome); // Nome do técnico
            HttpContext.Session.SetString("Email", tecnico.Email);   // Email do técnico

            // Retorna JSON informando sucesso e a URL de redirecionamento
            return Json(new
            {
                success = true,
                message = $"Bem-vindo(a), {tecnico.Nome}!",
                redirectUrl = "/Suporte/Index" // Página que será aberta após login
            });
        }

        // ================= LOGOUT =================
        // Limpa a sessão e retorna para a página de login
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Remove todas as variáveis da sessão
            return RedirectToAction("Login"); // Redireciona para login
        }
    }
}
