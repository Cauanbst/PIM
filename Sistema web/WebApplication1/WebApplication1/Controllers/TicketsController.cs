using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using WebApplication1.Data;
using WebApplication1.Hubs;
using WebApplication1.Models;
namespace WebApplication1.Controllers
{
    /// <summary>
    /// ✅ Controller responsável por todo o gerenciamento de tickets (chamados)
    /// Inclui:
    /// - Criação de tickets (com IA)
    /// - Início e fechamento de atendimentos
    /// - Comunicação com SignalR
    /// - Registro detalhado de logs
    /// </summary>
    public class TicketsController : Controller
    {
        // =======================
        // DEPENDÊNCIAS
        // =======================
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly HttpClient _httpClient;
        private readonly ILogger<TicketsController> _logger;

        // =======================
        // CONSTRUTOR
        // =======================
        public TicketsController(ApplicationDbContext context, IHubContext<ChatHub> hubContext, ILogger<TicketsController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _logger = logger;
        }

        // =======================
        // VIEW: CRIAR NOVO TICKET
        // =======================
        [HttpGet]
        public IActionResult Novo()
        {
            _logger.LogInformation("🟢 Página de criação de ticket acessada.");
            return View();
        }

        // =======================
        // MODELO: REQUEST DE CRIAÇÃO
        // =======================
        public class TicketRequest
        {
            [Required(ErrorMessage = "O título é obrigatório.")]
            public string Title { get; set; } = null!;

            [Required(ErrorMessage = "A descrição é obrigatória.")]
            public string Description { get; set; } = null!;

            public string? Criador { get; set; }
        }


        [HttpGet]
        public async Task<IActionResult> MeusChamados()
        {
            // 🔹 Tenta pegar o usuário da sessão
            var usuario = HttpContext.Session.GetString("Username");

            // 🔹 Se a sessão expirou, tenta pegar dos cookies
            if (string.IsNullOrEmpty(usuario))
                usuario = Request.Cookies["Username"];

            // 🔹 Se mesmo assim não tiver, redireciona pro login
            if (string.IsNullOrEmpty(usuario))
                return RedirectToAction("Login", "User");

            // 🔹 Busca os tickets do usuário
            var tickets = await _context.Tickets
                .Include(t => t.Criador)
                .Include(t => t.Tecnico)
                .Where(t => t.Criador.Username == usuario)
                .OrderByDescending(t => t.DataCriacao)
                .ToListAsync();

            ViewBag.Usuario = usuario;
            return View(tickets);
        }



        // ===============================================================
        // MÉTODO PRINCIPAL: CRIAÇÃO DE NOVO TICKET
        // ===============================================================
        [HttpPost]
        [ActionName("Novo")]
        public async Task<IActionResult> NovoPost([FromBody] TicketRequest request)
        {
            _logger.LogInformation("📩 Requisição recebida para novo ticket: {@Request}", request);

            // 🔸 1️⃣ Validação do modelo recebido
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ Modelo inválido na criação do ticket.");
                return Json(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            // 🔸 2️⃣ Impede técnico de criar ticket sem cliente
            if (HttpContext.Session?.GetString("Perfil") == "Tecnico" && string.IsNullOrWhiteSpace(request.Criador))
            {
                _logger.LogWarning("⚠️ Técnico tentou criar ticket sem informar cliente.");
                return Json(new { success = false, error = "Chamado precisa ser criado com nome do cliente." });
            }

            try
            {
                // 🔸 3️⃣ Busca técnicos no banco
                _logger.LogInformation("🔍 Buscando técnicos disponíveis no banco...");
                var tecnicos = await _context.TbTecnico.ToListAsync();

                if (tecnicos == null || tecnicos.Count == 0)
                {
                    _logger.LogError("🚫 Nenhum técnico disponível no sistema.");
                    return Json(new { success = false, error = "Nenhum técnico disponível no sistema." });
                }

                // 🔸 4️⃣ Seleciona técnico usando IA (com fallback)
                Technician? tecnico = null;
                try
                {
                    _logger.LogInformation("🤖 Classificando técnico mais adequado com IA...");
                    tecnico = await ClassificarComIA(request.Description, tecnicos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erro ao usar IA — usando fallback para o primeiro técnico disponível.");
                    tecnico = tecnicos.FirstOrDefault();
                }

                // 🔸 5️⃣ Define nome e especialidade do técnico
                var nomeTecnico = tecnico?.Nome ?? tecnicos.First().Nome;
                var especialidade = tecnico?.Especialidade ?? tecnicos.First().Especialidade;
                _logger.LogInformation("✅ Técnico selecionado: {Tecnico} ({Especialidade})", nomeTecnico, especialidade);

                // 🔸 6️⃣ Identifica o usuário criador
                var nomeUsuario = !string.IsNullOrWhiteSpace(request.Criador)
                    ? WebUtility.HtmlDecode(request.Criador)
                    : WebUtility.HtmlDecode(HttpContext.Session?.GetString("Username"));

                if (string.IsNullOrWhiteSpace(nomeUsuario))
                {
                    _logger.LogError("❌ Usuário criador não identificado.");
                    return Json(new { success = false, error = "Não foi possível identificar o usuário criador do chamado." });
                }

                _logger.LogInformation("👤 Usuário criador identificado: {Usuario}", nomeUsuario);

                // 🔸 7️⃣ Busca técnico e usuário no banco
                var tecnicoSelecionado = await _context.TbTecnico.FirstOrDefaultAsync(t => t.Nome == nomeTecnico);
                var usuarioCriador = await _context.Users.FirstOrDefaultAsync(u => u.Username == nomeUsuario);

                if (tecnicoSelecionado == null)
                {
                    _logger.LogError("❌ Técnico '{NomeTecnico}' não encontrado no banco.", nomeTecnico);
                    return Json(new { success = false, error = "Técnico não encontrado no banco de dados." });
                }

                if (usuarioCriador == null)
                {
                    _logger.LogError("❌ Usuário '{NomeUsuario}' não encontrado no banco.", nomeUsuario);
                    return Json(new { success = false, error = "Usuário criador não encontrado no banco de dados." });
                }

                // 🔸 8️⃣ Cria e salva o ticket
                var ticket = new Ticket
                {
                    Title = request.Title,
                    Description = request.Description,
                    TecnicoId = tecnicoSelecionado.Id,
                    CriadorId = usuarioCriador.Id,
                    Status = "Aberto",
                    DataCriacao = DateTime.Now
                };

                _logger.LogInformation("💾 Salvando novo ticket no banco de dados...");
                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Ticket {Id} criado com sucesso.", ticket.Id);

                // 🔸 9️⃣ Notifica via SignalR
                await _hubContext.Clients.All.SendAsync("NovoChamado", new
                {
                    id = ticket.Id,
                    title = ticket.Title,
                    description = ticket.Description,
                    tecnico = tecnicoSelecionado.Nome,
                    criador = usuarioCriador.Username,
                    dataCriacao = ticket.DataCriacao.ToString("yyyy-MM-ddTHH:mm:sszzz")
                });

                _logger.LogInformation("📡 Notificação enviada via SignalR.");

                // 🔸 🔟 Gera link do chat
                var chatClienteUrl = Url.Action("ChatCliente", "Chat", new { ticketId = ticket.Id, tecnico = tecnicoSelecionado.Nome })
                    ?? $"/Chat/ChatCliente?ticketId={ticket.Id}&tecnico={tecnicoSelecionado.Nome}";

                _logger.LogInformation("🔗 Link do chat gerado: {Url}", chatClienteUrl);

                // ✅ Retorna resultado JSON para o front-end
                return Json(new
                {
                    success = true,
                    tecnicoResponsavel = nomeTecnico,
                    especialidade,
                    usuario = nomeUsuario,
                    ticketId = ticket.Id,
                    redirectUrl = chatClienteUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Erro interno ao criar ticket.");
                return Json(new { success = false, error = "Erro interno no servidor.", detail = ex.Message });
            }
        }


        // ===============================================================
        // Classificar com IA
        // ===============================================================

        private async Task<Technician?> ClassificarComIA(string problema, List<Technician> tecnicos)
        {
            try
            {
                Console.WriteLine("🧠===============================================");
                Console.WriteLine("🔍 Iniciando classificação do técnico com IA...");
                Console.WriteLine("📨 Problema informado: " + problema);
                Console.WriteLine("===============================================\n");

                // 1️⃣ Detecta especialidade localmente
                var especialidadeDetectada = DetectarEspecialidade(problema);
                Console.WriteLine($"📌 Especialidade detectada localmente: {especialidadeDetectada}");

                // 2️⃣ Normaliza variações comuns
                string especialidadeNormalizada = especialidadeDetectada.ToLower();

                if (especialidadeNormalizada.Contains("software") || especialidadeNormalizada.Contains("aplicativo"))
                    especialidadeNormalizada = "Software e Aplicativo";
                else if (especialidadeNormalizada.Contains("hardware") || especialidadeNormalizada.Contains("equipamento"))
                    especialidadeNormalizada = "Hardware";
                else if (especialidadeNormalizada.Contains("rede") || especialidadeNormalizada.Contains("internet"))
                    especialidadeNormalizada = "Redes";

                Console.WriteLine($"🔧 Especialidade normalizada: {especialidadeNormalizada}");

                // 3️⃣ Filtra técnicos considerando combinações e variações
                var tecnicosDaEspecialidade = tecnicos
                    .Where(t =>
                        t.Especialidade != null &&
                        (t.Especialidade.Contains(especialidadeNormalizada, StringComparison.OrdinalIgnoreCase)
                        || especialidadeNormalizada.Contains(t.Especialidade, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (!tecnicosDaEspecialidade.Any())
                {
                    Console.WriteLine("⚠️ Nenhum técnico disponível para a especialidade detectada.");
                    return null;
                }

                Console.WriteLine("👷 Técnicos disponíveis para a especialidade detectada:");
                foreach (var t in tecnicosDaEspecialidade)
                {
                    Console.WriteLine($"- {t.Nome} ({t.Especialidade})");
                }

                // 4️⃣ Conta tickets abertos por técnico
                var ticketsAbertos = await _context.Tickets
                    .Where(t => t.Status == "Aberto" || t.Status == "Em Andamento")
                    .GroupBy(t => t.TecnicoId)
                    .Select(g => new { TecnicoId = g.Key, Count = g.Count() })
                    .ToListAsync();

                Console.WriteLine("\n📊 Situação atual de carga de tickets:");
                foreach (var t in tecnicosDaEspecialidade)
                {
                    var count = ticketsAbertos.FirstOrDefault(x => x.TecnicoId == t.Id)?.Count ?? 0;
                    Console.WriteLine($"- {t.Nome} ({t.Especialidade}): {count} ticket(s) abertos");
                }

                // 5️⃣ Cria prompt para IA
                var prompt = $@"
Escolha o técnico mais adequado com base na carga de trabalho e especialidade.

Técnicos disponíveis:
{string.Join("\n", tecnicosDaEspecialidade.Select(t =>
                {
                    var count = ticketsAbertos.FirstOrDefault(x => x.TecnicoId == t.Id)?.Count ?? 0;
                    return $"{t.Nome} ({t.Especialidade}) - {count} tickets abertos";
                }))}

Problema: {problema}

Responda apenas com o nome do técnico mais indicado.
";

                Console.WriteLine("\n📤 Enviando prompt para IA...");
                Console.WriteLine("===============================================");
                Console.WriteLine(prompt);
                Console.WriteLine("===============================================\n");

                // 6️⃣ Envia prompt para a IA
                var requestBody = new
                {
                    model = "llama3.2",
                    prompt,
                    stream = false,
                    options = new { temperature = 0 }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);

                string? respostaIA = null;
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseContent);

                    if (doc.RootElement.TryGetProperty("response", out var responseProp))
                    {
                        respostaIA = responseProp.GetString()?.Trim();
                        Console.WriteLine($"🤖 Resposta da IA: {respostaIA}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Falha na requisição para IA: {response.StatusCode}");
                }

                // 7️⃣ Identifica técnico retornado pela IA
                Technician? tecnicoEscolhido = null;
                if (!string.IsNullOrWhiteSpace(respostaIA))
                {
                    tecnicoEscolhido = tecnicosDaEspecialidade
                        .FirstOrDefault(t => respostaIA.Contains(t.Nome, StringComparison.OrdinalIgnoreCase));

                    if (tecnicoEscolhido != null)
                    {
                        var count = ticketsAbertos.FirstOrDefault(x => x.TecnicoId == tecnicoEscolhido.Id)?.Count ?? 0;
                        Console.WriteLine($"✅ Técnico escolhido pela IA: {tecnicoEscolhido.Nome} ({count} tickets abertos)");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ IA respondeu com nome não encontrado entre os técnicos disponíveis.");
                    }
                }

                // 8️⃣ Fallback (escolhe técnico com menos tickets)
                var tecnicoMenosTickets = tecnicosDaEspecialidade
                    .OrderBy(t => ticketsAbertos.FirstOrDefault(x => x.TecnicoId == t.Id)?.Count ?? 0)
                    .FirstOrDefault();

                if (tecnicoEscolhido == null)
                {
                    tecnicoEscolhido = tecnicoMenosTickets;
                    Console.WriteLine($"🔁 IA não respondeu corretamente. Selecionando técnico com menor carga: {tecnicoEscolhido?.Nome}");
                }
                else
                {
                    var cargaEscolhido = ticketsAbertos.FirstOrDefault(x => x.TecnicoId == tecnicoEscolhido.Id)?.Count ?? 0;
                    var cargaMenor = ticketsAbertos.FirstOrDefault(x => x.TecnicoId == tecnicoMenosTickets?.Id)?.Count ?? 0;

                    if (cargaEscolhido > cargaMenor)
                    {
                        Console.WriteLine($"⚠️ IA escolheu incorretamente {tecnicoEscolhido.Nome}. Corrigindo para {tecnicoMenosTickets?.Nome}.");
                        tecnicoEscolhido = tecnicoMenosTickets;
                    }
                }

                Console.WriteLine("✅ Técnico final escolhido: " + tecnicoEscolhido?.Nome);
                Console.WriteLine("🧠 Classificação concluída com sucesso!\n");

                return tecnicoEscolhido;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Erro em ClassificarComIA: " + ex.Message);
                return null;
            }
        }


        // ===============================================================
        // FUNÇÃO AUXILIAR: DETECTA ESPECIALIDADE LOCAL
        // ===============================================================
        private string DetectarEspecialidade(string problema)
        {
            var texto = problema.ToLower();

            Console.WriteLine("🔎 Detectando especialidade com base no texto do problema...");

            // 🔵 REDES
            if (texto.Contains("internet") || texto.Contains("wi-fi") || texto.Contains("wifi") ||
                texto.Contains("rede") || texto.Contains("modem") || texto.Contains("roteador") ||
                texto.Contains("conexão") || texto.Contains("conectar") || texto.Contains("ping") ||
                texto.Contains("ip") || texto.Contains("dns") || texto.Contains("servidor") ||
                texto.Contains("cabo de rede") || texto.Contains("lan") || texto.Contains("wan") ||
                texto.Contains("switch") || texto.Contains("firewall") || texto.Contains("porta de rede") ||
                texto.Contains("sem sinal") || texto.Contains("sem acesso") || texto.Contains("perda de pacote"))
            {
                Console.WriteLine("📶 Especialidade detectada: Redes");
                return "Redes";
            }

            // 🟣 SOFTWARE E APLICATIVO
            if (texto.Contains("windows") || texto.Contains("programa") || texto.Contains("instalar") ||
                texto.Contains("atualizar") || texto.Contains("erro") || texto.Contains("sistema") ||
                texto.Contains("office") || texto.Contains("navegador") || texto.Contains("vírus") ||
                texto.Contains("excel") || texto.Contains("word") || texto.Contains("powerpoint") ||
                texto.Contains("aplicativo") || texto.Contains("app") || texto.Contains("software") ||
                texto.Contains("travando") || texto.Contains("lentidão") || texto.Contains("tela azul") ||
                texto.Contains("não abre") || texto.Contains("fechando sozinho") || texto.Contains("bug") ||
                texto.Contains("crash") || texto.Contains("instalação") || texto.Contains("licença") ||
                texto.Contains("registro") || texto.Contains("driver") || texto.Contains("compatibilidade"))
            {
                Console.WriteLine("💻 Especialidade detectada: Software e Aplicativo");
                return "Software e Aplicativo";
            }

            // 🟠 HARDWARE
            if (texto.Contains("mouse") || texto.Contains("teclado") || texto.Contains("monitor") ||
                texto.Contains("impressora") || texto.Contains("usb") || texto.Contains("placa") ||
                texto.Contains("fonte") || texto.Contains("hd") || texto.Contains("superaquecimento") ||
                texto.Contains("computador") || texto.Contains("notebook") || texto.Contains("não liga") ||
                texto.Contains("barulho") || texto.Contains("ventoinha") || texto.Contains("memória") ||
                texto.Contains("cabo") || texto.Contains("energia") || texto.Contains("bateria") ||
                texto.Contains("tela preta") || texto.Contains("sem imagem") || texto.Contains("led piscando") ||
                texto.Contains("conector") || texto.Contains("trincado") || texto.Contains("quebrado") ||
                texto.Contains("desligando sozinho") || texto.Contains("falha física"))
            {
                Console.WriteLine("🖥️ Especialidade detectada: Hardware");
                return "Hardware";
            }

            Console.WriteLine("⚙️ Nenhuma correspondência exata encontrada. Marcando como indefinido.");
            return "Indefinido";
        }




        // ===============================================================
        // 🔹 INICIAR TICKET
        // ===============================================================
        [HttpPost]
        public async Task<IActionResult> IniciarTicket(int id)
        {
            _logger.LogInformation("🚀 Tentando iniciar ticket {Id}", id);
            var ticket = await _context.Tickets.FindAsync(id);

            if (ticket == null)
                return Json(new { success = false, message = "Ticket não encontrado" });

            if (ticket.Status == "Em Andamento")
                return Json(new { success = false, message = "Ticket já em andamento" });

            if (ticket.Status == "Fechado")
                return Json(new { success = false, message = "Ticket já foi fechado" });

            ticket.InicioAtendimento ??= DateTime.Now;
            ticket.Status = "Em Andamento";
            ticket.FimAtendimento = null;
            ticket.TempoAtendimento = null;

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Ticket {Id} iniciado com sucesso.", id);

            var chatUrl = Url.Action("AbrirChat", "Chat", new { ticketId = id, modo = "escrita" });
            return Json(new { success = true, status = ticket.Status, redirectUrl = chatUrl });
        }

        // ===============================================================
        // 🔹 FECHAR TICKET
        // ===============================================================
        [HttpPost]
        [Route("Tickets/FecharTicket/{id}")]
        public async Task<JsonResult> FecharTicket(int id)
        {
            _logger.LogInformation("🧩 Iniciando fechamento do ticket {Id}", id);

            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.Tecnico)
                    .Include(t => t.Criador)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                    return Json(new { success = false, message = "Ticket não encontrado." });

                if (!ticket.InicioAtendimento.HasValue)
                    return Json(new { success = false, message = "O atendimento ainda não foi iniciado." });

                // ✅ Corrigido: padroniza o status
                ticket.FimAtendimento = DateTime.Now;
                ticket.Status = "Finalizado";
                ticket.TempoAtendimento = ticket.FimAtendimento.Value - ticket.InicioAtendimento.Value;

                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Ticket {Id} marcado como FECHADO no banco.", id);

                string grupo = $"ticket_{id}";

                // 📡 Notifica o grupo que o ticket foi encerrado
                await _hubContext.Clients.Group(grupo).SendAsync("ChatEncerrado", new
                {
                    ticketId = ticket.Id,
                    status = ticket.Status,
                    tempoAtendimento = ticket.TempoAtendimento?.ToString(@"hh\\:mm\\:ss") ?? "00:00:00",
                    tecnico = ticket.Tecnico?.Nome ?? "Técnico",
                    cliente = ticket.Criador?.Username ?? "Cliente",
                    mensagem = "✅ O atendimento foi encerrado com sucesso."
                });

                // 🚪 Novo: também fecha o chat do técnico automaticamente
                await _hubContext.Clients.Group(grupo).SendAsync("FecharChatTecnico", ticket.Id);

                return Json(new
                {
                    success = true,
                    id = ticket.Id,
                    status = ticket.Status,
                    tempoAtendimento = ticket.TempoAtendimento?.ToString(@"hh\\:mm\\:ss") ?? "00:00:00"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Erro ao fechar ticket {Id}.", id);
                return Json(new { success = false, message = "Erro interno: " + ex.Message });
            }
        }




        [HttpGet]
        [Route("Tickets/ListarPorCliente")]
        public async Task<IActionResult> ListarPorCliente([FromQuery] string usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario))
                return BadRequest(new { success = false, message = "Parâmetro 'usuario' é obrigatório." });

            try
            {
                var tickets = await _context.Tickets
                    .Include(t => t.Criador)
                    .Include(t => t.Tecnico)
                    .OrderByDescending(t => t.DataCriacao)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.Description,
                        t.Status,
                        Tecnico = t.Tecnico != null ? t.Tecnico.Nome : null,
                        Criador = t.Criador != null ? t.Criador.Username : null,
                        DataCriacao = t.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss"),
                        InicioAtendimento = t.InicioAtendimento.HasValue ? t.InicioAtendimento.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                        FimAtendimento = t.FimAtendimento.HasValue ? t.FimAtendimento.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                        TempoAtendimento = t.TempoAtendimento.HasValue ? t.TempoAtendimento.Value.ToString(@"hh\:mm\:ss") : null
                    })
                    .ToListAsync();

                var ticketsFiltrados = tickets
                    .Where(t => t.Criador != null && t.Criador.Equals(usuario, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                _logger.LogInformation("Tickets filtrados para {usuario}: {count}", usuario, ticketsFiltrados.Count);

                if (!ticketsFiltrados.Any())
                    _logger.LogInformation("Nenhum ticket encontrado para o usuário {usuario}", usuario);

                return Json(new { success = true, tickets = ticketsFiltrados });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar tickets para o usuário {usuario}", usuario);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [Route("Tickets/ReabrirChatMobile/{id}")]
        public async Task<IActionResult> ReabrirChatMobile(int id, [FromQuery] string tecnico)
        {
            if (string.IsNullOrWhiteSpace(tecnico))
                return BadRequest(new { success = false, message = "Parâmetro 'tecnico' é obrigatório." });

            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.Tecnico)
                    .Include(t => t.Criador)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                    return NotFound(new { success = false, message = $"Ticket {id} não encontrado." });

                var tecnicoObj = await _context.TbTecnico.FirstOrDefaultAsync(t => t.Nome == tecnico);
                if (tecnicoObj == null)
                    return NotFound(new { success = false, message = $"Técnico '{tecnico}' não encontrado." });

                ticket.Status = "Em Andamento";
                ticket.TecnicoId = tecnicoObj.Id;
                ticket.FimAtendimento = null;
                ticket.TempoAtendimento = null;
                ticket.InicioAtendimento ??= DateTime.Now;

                await _context.SaveChangesAsync();

                var mensagensTexto = _context.Mensagens
                    .Where(m => m.TicketId == id)
                    .Select(m => new
                    {
                        Id = m.Id,
                        Remetente = (string?)m.Remetente,
                        Conteudo = m.Conteudo,
                        Tipo = (
                            m.Conteudo.EndsWith(".jpg") || m.Conteudo.EndsWith(".png") ||
                            m.Conteudo.EndsWith(".jpeg") || m.Conteudo.EndsWith(".gif")
                            ? "imagem"
                            : (m.Conteudo.Contains("/uploads/") ? "arquivo" : "texto")
                        ),
                        NomeOriginal = (string?)"",
                        DataEnvio = m.DataEnvio
                    });

                var mensagensArquivos = _context.ChatFiles
                    .Where(f => f.TicketId == id)
                    .Select(f => new
                    {
                        Id = f.Id,
                        Remetente = (string?)f.UploadedByName,
                        Conteudo = f.FileUrl,
                        Tipo = (
                            f.FileName.EndsWith(".jpg") || f.FileName.EndsWith(".png") ||
                            f.FileName.EndsWith(".jpeg") || f.FileName.EndsWith(".gif") ||
                            f.FileName.EndsWith(".webp")
                            ? "imagem"
                            : "arquivo"
                        ),
                        NomeOriginal = (string?)f.FileName,
                        DataEnvio = f.UploadedAt
                    });

                var todasMensagens = await mensagensTexto
                    .Concat(mensagensArquivos)
                    .OrderBy(m => m.DataEnvio)
                    .Select(m => new
                    {
                        m.Id,
                        m.Remetente,
                        Conteudo = m.Tipo != "texto" ? $"file:{m.Conteudo}" : m.Conteudo,
                        m.Tipo,
                        m.NomeOriginal,
                        DataEnvio = m.DataEnvio.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .ToListAsync();

                string grupo = $"ticket_{id}";
                await _hubContext.Clients.Group(grupo).SendAsync("ChatReaberto", new
                {
                    ticketId = id,
                    tecnico = tecnicoObj.Nome,
                    mensagem = "🔁 O chat foi reaberto pelo cliente."
                });

                return Json(new
                {
                    success = true,
                    ticket = new
                    {
                        ticket.Id,
                        ticket.Title,
                        ticket.Description,
                        ticket.Status,
                        Tecnico = tecnicoObj.Nome,
                        Criador = ticket.Criador?.Username,
                        DataCriacao = ticket.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss")
                    },
                    mensagens = todasMensagens
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reabrir ticket {id}", id);
                return Json(new { success = false, error = ex.Message });
            }
        }


        [HttpGet("Tickets/VisualizarChatMobile/{id}")]
        public IActionResult VisualizarChatMobile(int id)
        {
            try
            {
                var ticket = _context.Tickets
                    .Include(t => t.Tecnico)
                    .Include(t => t.Criador)
                    .FirstOrDefault(t => t.Id == id);

                if (ticket == null)
                    return NotFound(new { success = false, message = $"Ticket {id} não encontrado." });

                var mensagensTexto = _context.Mensagens
                    .Where(m => m.TicketId == id)
                    .Select(m => new
                    {
                        Id = m.Id,
                        Remetente = (string?)m.Remetente,
                        Conteudo = m.Conteudo,
                        Tipo = (
                            m.Conteudo.EndsWith(".jpg") || m.Conteudo.EndsWith(".png") ||
                            m.Conteudo.EndsWith(".jpeg") || m.Conteudo.EndsWith(".gif")
                            ? "imagem"
                            : (m.Conteudo.Contains("/uploads/") ? "arquivo" : "texto")
                        ),
                        NomeOriginal = (string?)"",
                        DataEnvio = m.DataEnvio
                    });

                var mensagensArquivos = _context.ChatFiles
                    .Where(f => f.TicketId == id)
                    .Select(f => new
                    {
                        Id = f.Id,
                        Remetente = (string?)f.UploadedByName,
                        Conteudo = f.FileUrl,
                        Tipo = (
                            f.FileName.EndsWith(".jpg") || f.FileName.EndsWith(".png") ||
                            f.FileName.EndsWith(".jpeg") || f.FileName.EndsWith(".gif") ||
                            f.FileName.EndsWith(".webp")
                            ? "imagem"
                            : "arquivo"
                        ),
                        NomeOriginal = (string?)f.FileName,
                        DataEnvio = f.UploadedAt
                    });

                var todasMensagens = mensagensTexto
                    .Concat(mensagensArquivos)
                    .OrderBy(m => m.DataEnvio)
                    .Select(m => new
                    {
                        m.Id,
                        m.Remetente,
                        Conteudo = m.Tipo != "texto" ? $"file:{m.Conteudo}" : m.Conteudo,
                        m.Tipo,
                        m.NomeOriginal,
                        DataEnvio = m.DataEnvio.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .ToList();

                var resposta = new
                {
                    success = true,
                    ticket = new
                    {
                        ticket.Id,
                        ticket.Title,
                        ticket.Description,
                        ticket.Status,
                        Tecnico = ticket.Tecnico?.Nome,
                        Criador = ticket.Criador?.Username,
                        DataCriacao = ticket.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss"),
                        InicioAtendimento = ticket.InicioAtendimento?.ToString("yyyy-MM-dd HH:mm:ss"),
                        FimAtendimento = ticket.FimAtendimento?.ToString("yyyy-MM-dd HH:mm:ss"),
                        TempoAtendimento = ticket.TempoAtendimento?.ToString(@"hh\:mm\:ss")
                    },
                    mensagens = todasMensagens
                };

                return Json(resposta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao visualizar chat do ticket {id}", id);
                return Json(new { success = false, error = ex.Message });
            }
        }


        public static string RemoverAcentos(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}