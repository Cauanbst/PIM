using Microsoft.AspNetCore.Mvc;                // Importa o namespace responsável pelo MVC (Controllers, Views etc.)
using Microsoft.EntityFrameworkCore;           // Importa funcionalidades do Entity Framework Core (consultas, Include, async, etc.)
using WebApplication1.Data;                    // Importa a classe ApplicationDbContext onde estão as tabelas mapeadas pelo EF

namespace WebApplication1.Controllers           // Define o namespace do controller
{
    public class SuporteController : Controller // Define a classe SuporteController que herda de Controller (MVC)
    {
        private readonly ApplicationDbContext _context;             // Campo privado que guarda o contexto do banco de dados
        private readonly ILogger<SuporteController> _logger;         // Logger para registrar informações e erros

        // =========================================================
        // Construtor do controller: recebe o contexto e o logger
        // =========================================================
        public SuporteController(ApplicationDbContext context, ILogger<SuporteController> logger)
        {
            _context = context;     // Armazena a instância do contexto recebida
            _logger = logger;       // Armazena a instância do logger recebida
        }

        // =========================================================
        // GET: /Suporte
        // Método principal da tela de suporte do técnico logado
        // =========================================================
        public async Task<IActionResult> Index()
        {
            // ---------------------------------------------------------
            // Obtém o nome do técnico logado a partir da sessão HTTP.
            // A sessão foi criada no login do técnico.
            // "Username" é a chave usada na hora do login.
            // ---------------------------------------------------------
            var nomeTecnico = HttpContext.Session.GetString("Username");

            // Se o nome for nulo, vazio ou sessão expirada
            if (string.IsNullOrEmpty(nomeTecnico))
            {
                // Mensagem exibida uma única vez após o Redirect (TempData)
                TempData["Error"] = "Sessão expirada ou técnico não identificado.";

                // Redireciona para a tela de login de técnico
                return RedirectToAction("Login", "Tecnico");
            }

            // ---------------------------------------------------------
            // Busca o registro do técnico no banco, usando o nome obtido
            // TbTecnico é o DbSet configurado no ApplicationDbContext
            // ---------------------------------------------------------
            var tecnico = await _context.TbTecnico
                .FirstOrDefaultAsync(t => t.Nome == nomeTecnico);

            // Caso o técnico não exista no banco
            if (tecnico == null)
            {
                TempData["Error"] = "Técnico não encontrado no sistema.";
                return RedirectToAction("Login", "Tecnico");
            }

            // ---------------------------------------------------------
            // Obtém o ID do técnico encontrado no banco.
            // Isso é fundamental para buscar os tickets atribuídos a ele.
            // Importante: A propriedade correta é Id (não TecnicoId).
            // ---------------------------------------------------------
            var tecnicoId = tecnico.Id;

            // Loga no console/arquivo a informação do login do técnico
            _logger.LogInformation($"✅ Técnico logado: {tecnico.Nome} (ID: {tecnicoId})");

            // ---------------------------------------------------------
            // Busca dos chamados ABERTOS atribuídos ao técnico logado.
            // Include(t => t.Criador) → faz join trazendo os dados do usuário que criou o ticket
            // Include(t => t.Tecnico) → traz informações do técnico responsável
            // Where filtra por ID do técnico e status "Aberto"
            // OrderByDescending → ordena do mais novo para o mais antigo
            // ---------------------------------------------------------
            var chamadosAbertos = await _context.Tickets
                .Include(t => t.Criador)
                .Include(t => t.Tecnico)
                .Where(t => t.TecnicoId == tecnicoId && t.Status == "Aberto")
                .OrderByDescending(t => t.DataCriacao)
                .ToListAsync();

            // ---------------------------------------------------------
            // Busca dos chamados em ANDAMENTO do técnico logado.
            // Mesmo padrão da consulta acima.
            // ---------------------------------------------------------
            var chamadosAndamento = await _context.Tickets
                .Include(t => t.Criador)
                .Include(t => t.Tecnico)
                .Where(t => t.TecnicoId == tecnicoId && t.Status == "Em Andamento")
                .OrderByDescending(t => t.DataCriacao)
                .ToListAsync();

            // ---------------------------------------------------------
            // Busca dos chamados FINALIZADOS pelo técnico logado.
            // Mesmo padrão anterior.
            // ---------------------------------------------------------
            var chamadosFechados = await _context.Tickets
                .Include(t => t.Criador)
                .Include(t => t.Tecnico)
                .Where(t => t.TecnicoId == tecnicoId && t.Status == "Finalizado")
                .OrderByDescending(t => t.DataCriacao)
                .ToListAsync();

            // ---------------------------------------------------------
            // Envia as listas para a View usando ViewBag.
            // chamadosAbertos é enviado diretamente no return.
            // ---------------------------------------------------------
            ViewBag.Andamento = chamadosAndamento;   // Chamados em andamento
            ViewBag.Fechados = chamadosFechados;     // Chamados finalizados
            ViewBag.UsuarioLogado = tecnico.Nome;    // Nome do técnico logado

            // ---------------------------------------------------------
            // Retorna a View passando apenas os chamados Abertos como modelo.
            // Os outros são recebidos via ViewBag.
            // ---------------------------------------------------------
            return View(chamadosAbertos);
        }
    }
}
