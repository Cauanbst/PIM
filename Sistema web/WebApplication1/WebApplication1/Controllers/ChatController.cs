using Microsoft.AspNetCore.Mvc;                 // Importa funcionalidades MVC (Controllers, Views)
using Microsoft.EntityFrameworkCore;            // Importa suporte ao Entity Framework Core (BD)
using WebApplication1.Data;                     // Importa a classe de contexto do banco de dados
using WebApplication1.Models;                   // Importa as Models (Ticket, Mensagem, etc)
using Microsoft.AspNetCore.SignalR;            // Suporte ao SignalR (chat em tempo real)
using WebApplication1.Hubs;                    // Hub do chat


namespace WebApplication1.Controllers
{
    public class ChatController : Controller
    {
        // ============================================================
        // INJEÇÃO DE DEPENDÊNCIAS (Contexto, Hub e Ambiente web)
        // ============================================================

        private readonly ApplicationDbContext _context;     // Acesso ao banco de dados
        private readonly IHubContext<ChatHub> _hubContext;  // Acesso ao SignalR para enviar msgs
        private readonly IWebHostEnvironment _env;          // Acesso a pasta wwwroot e caminhos

        // Construtor recebe e injeta dependências automaticamente
        public ChatController(ApplicationDbContext context, IHubContext<ChatHub> hubContext, IWebHostEnvironment env)
        {
            _context = context;
            _hubContext = hubContext;
            _env = env;
        }

        // ============================================================
        // CHAT DO CLIENTE — ABRIR TELA DO CLIENTE
        // ============================================================

        public async Task<IActionResult> ChatCliente(int ticketId, string tecnico)
        {
            // Busca um ticket no banco de dados com o ID informado
            // Inclui Criador (Cliente) e Técnico associado
            var ticket = await _context.Tickets
                .Include(t => t.Criador)
                .Include(t => t.Tecnico)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            // Caso não exista, retorna erro 404
            if (ticket == null)
                return NotFound();

            // Define se o chat deve ser somente leitura
            bool modoLeitura = ticket.Status == "Finalizado";

            // Passa informações para a View
            ViewBag.Tecnico = ticket.Tecnico?.Nome ?? "Aguardando técnico";
            ViewBag.UsuarioLogado = ticket.Criador?.Username ?? "Cliente";
            ViewBag.TicketId = ticket.Id;
            ViewBag.ModoLeitura = modoLeitura;
            ViewBag.Papel = "cliente";
            ViewBag.Status = ticket.Status;

            // Abre a View ChatClie.cshtml
            return View("ChatClie", ticket);
        }

        // ============================================================
        // CLIENTE: VER APENAS A CONVERSA (MODO LEITURA)
        // ============================================================

        public async Task<IActionResult> VisualizarConversaCliente(int ticketId, string tecnico)
        {
            // Busca ticket com informações relacionadas
            var ticket = await _context.Tickets
                .Include(t => t.Criador)
                .Include(t => t.Tecnico)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            // Caso não exista
            if (ticket == null)
                return NotFound();

            // Carrega todas as mensagens desse ticket
            var mensagens = await _context.Mensagens
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.DataEnvio)
                .ToListAsync();

            // Passa informações para a View
            ViewBag.Tecnico = ticket.Tecnico?.Nome ?? "Aguardando técnico";
            ViewBag.Cliente = ticket.Criador?.Username ?? "Cliente";
            ViewBag.UsuarioLogado = "Cliente";
            ViewBag.TicketId = ticket.Id;
            ViewBag.ModoLeitura = true;
            ViewBag.Mensagens = mensagens;
            ViewBag.Papel = "cliente";

            return View("ChatClie", ticket);
        }

        // ============================================================
        // CHAT DO TÉCNICO — ABRIR CHAT DO TÉCNICO
        // ============================================================

        public async Task<IActionResult> AbrirChat(int ticketId, string modo = "leitura")
        {
            // Carrega ticket + cliente + técnico
            var ticket = await _context.Tickets
                .Include(t => t.Criador)
                .Include(t => t.Tecnico)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            // Caso não encontre, volta para a lista de tickets
            if (ticket == null)
                return RedirectToAction("Index", "Suporte");

            // Se ticket está aberto, muda para "Em andamento"
            if (ticket.Status == "Aberto")
            {
                ticket.Status = "Em Andamento";
                ticket.InicioAtendimento = DateTime.Now;

                // Caso não tenha técnico atribuído, atribui o técnico logado
                if (ticket.TecnicoId == null)
                {
                    var tecnicoEmail = HttpContext.Session.GetString("Username") ?? "tecnico@suporte.com";
                    var tecnico = await _context.TbTecnico.FirstOrDefaultAsync(t => t.Email == tecnicoEmail);

                    if (tecnico != null)
                        ticket.TecnicoId = tecnico.Id;
                }

                await _context.SaveChangesAsync();
            }

            // Carrega mensagens do ticket
            var mensagens = await _context.Mensagens
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.DataEnvio)
                .ToListAsync();

            // Seta as informações pra View
            ViewBag.TicketId = ticket.Id;
            ViewBag.Cliente = ticket.Criador?.Username ?? "Cliente";
            ViewBag.Tecnico = ticket.Tecnico?.Nome ?? "Técnico";
            ViewBag.Titulo = ticket.Title;
            ViewBag.Mensagens = mensagens;
            ViewBag.ModoLeitura = ticket.Status == "Finalizado" || modo.ToLower() == "leitura";

            ViewBag.Papel = "tecnico"; // força papel técnico na view

            return View("Chat");
        }

        // ============================================================
        // LISTA DE TICKETS (GERAL)
        // ============================================================

        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets
                .Include(t => t.Criador)
                .Include(t => t.Tecnico)
                .OrderByDescending(t => t.DataCriacao)
                .ToListAsync();

            return View(tickets);
        }

        // ============================================================
        // SALVAR MENSAGEM DE TEXTO (CLIENTE OU TÉCNICO)
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> SalvarMensagem(int ticketId, string autor, string mensagem, string papel)
        {
            // impede enviar mensagem vazia
            if (string.IsNullOrWhiteSpace(mensagem))
                return BadRequest("Mensagem vazia.");

            // carrega ticket + cliente + técnico
            var ticket = await _context.Tickets
                .Include(t => t.Criador)
                .Include(t => t.Tecnico)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
                return NotFound("Ticket não encontrado.");

            // Cria objeto mensagem
            var novaMensagem = new Mensagem
            {
                Remetente = autor, // quem enviou
                // define o destinatário por papel
                Destinatario = papel.ToLower() == "cliente"
                    ? ticket.Tecnico?.Nome ?? "Técnico"
                    : ticket.Criador?.Username ?? "Cliente",

                Conteudo = mensagem,
                DataEnvio = DateTime.Now,
                TicketId = ticketId,
                ClienteId = ticket.Criador?.Id,
                TecnicoId = ticket.Tecnico?.Id
            };

            // Salva no banco
            _context.Mensagens.Add(novaMensagem);
            await _context.SaveChangesAsync();

            // Envia mensagem pelo SignalR
            await _hubContext.Clients.Group($"ticket_{ticketId}")
                .SendAsync("ReceberMensagem", new
                {
                    mensagem = novaMensagem.Conteudo,
                    papel,
                    data = novaMensagem.DataEnvio.ToString("yyyy-MM-dd HH:mm:ss")
                });

            // Retorna para o AJAX
            return Ok(new
            {
                success = true,
                mensagem = novaMensagem.Conteudo,
                hora = novaMensagem.DataEnvio.ToString("HH:mm")
            });
        }

        // ============================================================
        // UPLOAD DE ARQUIVOS (IMAGENS OU DOCUMENTOS)
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> UploadFile(int ticketId, int senderId, int receiverId, string senderName, IFormFile file)
        {
            // impede upload vazio
            if (file == null || file.Length == 0)
                return BadRequest("Nenhum arquivo enviado.");

            // Caminho da pasta /wwwroot/uploads
            string uploadsPath = Path.Combine(_env.WebRootPath, "uploads");

            // Se não existe, cria
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Gera nome único
            string fileName = $"{Guid.NewGuid()}_{file.FileName}";
            string filePath = Path.Combine(uploadsPath, fileName);

            // Salva arquivo no disco
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            string urlArquivo = $"/uploads/{fileName}";

            // busca ticket
            var ticket = await _context.Tickets
                .Include(t => t.Tecnico)
                .Include(t => t.Criador)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
                return NotFound("Ticket não encontrado.");

            // Identifica se quem manda é técnico ou cliente
            string papel = (ticket.Tecnico?.Id == senderId) ? "tecnico" : "cliente";

            // Identifica nome do remetente
            string autor = (papel == "tecnico")
                ? ticket.Tecnico?.Nome ?? senderName
                : ticket.Criador?.Username ?? senderName;

            // Cria registro do arquivo
            var chatFile = new ChatFile
            {
                FileName = file.FileName,
                FileUrl = urlArquivo,
                UploadedAt = DateTime.Now,
                UploadedToId = receiverId,
                UploadedByName = autor,
                TicketId = ticketId
            };

            // Define IDs dependendo do papel
            if (papel == "tecnico")
            {
                chatFile.UploadedByTecnicoId = senderId;
                chatFile.UploadedById = null;
            }
            else
            {
                chatFile.UploadedById = senderId;
                chatFile.UploadedByTecnicoId = null;
            }

            // Salva arquivo no banco
            _context.ChatFiles.Add(chatFile);
            await _context.SaveChangesAsync();

            // Define preview na conversa
            string conteudoMensagem =
                file.FileName.EndsWith(".png") || file.FileName.EndsWith(".jpg") ||
                file.FileName.EndsWith(".jpeg") || file.FileName.EndsWith(".gif") || file.FileName.EndsWith(".bmp")
                ? $"<img src='{urlArquivo}' alt='Imagem enviada' style='max-width:250px; border-radius:10px; margin:5px 0;' />"
                : $"<a href='{urlArquivo}' target='_blank' download style='text-decoration:none;color:#007bff;font-weight:bold;'>📎 Baixar arquivo</a>";

            // Cria nova "mensagem" representando o arquivo
            var novaMensagem = new Mensagem
            {
                Remetente = autor,
                Conteudo = conteudoMensagem,
                DataEnvio = DateTime.Now,
                TicketId = ticketId,
                ClienteId = ticket.Criador?.Id,
                TecnicoId = ticket.Tecnico?.Id
            };

            _context.Mensagens.Add(novaMensagem);
            await _context.SaveChangesAsync();

            // Envia via SignalR para atualizar a tela em tempo real
            await _hubContext.Clients.Group($"ticket_{ticketId}")
                .SendAsync("ReceberMensagem", new
                {
                    autor,
                    mensagem = conteudoMensagem,
                    data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    papel
                });

            return Ok(new
            {
                success = true,
                url = urlArquivo,
                mensagem = conteudoMensagem
            });
        }

        // ============================================================
        // CARREGAR TODAS AS MENSAGENS DO CHAT (TEXTO + ARQUIVOS)
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> CarregarMensagens(int ticketId)
        {
            // carrega ticket
            var ticket = await _context.Tickets
                .Include(t => t.Tecnico)
                .Include(t => t.Criador)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
                return NotFound();

            // nome do cliente para identificar papel
            var clienteNome = ticket.Criador?.Username?.ToLower() ?? "";

            // mensagens de texto
            var mensagensTexto = await _context.Mensagens
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.DataEnvio)
                .Select(m => new
                {
                    autor = m.Remetente ?? "Desconhecido",
                    mensagem = m.Conteudo,
                    papel = (m.Remetente != null && m.Remetente.ToLower() == clienteNome) ? "cliente" : "tecnico",
                    dataEnvio = m.DataEnvio.ToString("yyyy-MM-dd HH:mm:ss"),
                    tipo = "texto"
                })
                .ToListAsync();

            // arquivos enviados
            var mensagensArquivos = await _context.ChatFiles
                .Where(f => f.TicketId == ticketId)
                .OrderBy(f => f.UploadedAt)
                .Select(f => new
                {
                    autor = f.UploadedByName ?? "Desconhecido",
                    mensagem = $"file:{f.FileUrl}",
                    papel = (f.UploadedByName != null && f.UploadedByName.ToLower() == clienteNome) ? "cliente" : "tecnico",
                    dataEnvio = f.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    tipo = "arquivo"
                })
                .ToListAsync();

            // combina mensagens e ordena por data
            var todasMensagens = mensagensTexto
                .Concat(mensagensArquivos)
                .OrderBy(m => m.dataEnvio)
                .ToList();

            return Json(todasMensagens);
        }

        // ============================================================
        // VERIFICAR SE O TICKET ESTÁ FECHADO
        // ============================================================

        [HttpGet]
        public async Task<JsonResult> VerificarStatus(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);

            if (ticket == null)
                return Json(new { estaFechado = true });

            return Json(new
            {
                estaFechado = ticket.Status == "Fechado" || ticket.Status == "Finalizado"
            });
        }
    }
}
