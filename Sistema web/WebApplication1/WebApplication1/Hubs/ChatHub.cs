using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Hubs
{
    /// <summary>
    /// 💬 ChatHub — Responsável pela comunicação em tempo real entre cliente e técnico (SignalR)
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        // Construtor recebe o contexto do banco para salvar mensagens
        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================================
        // 🔹 Usuário entra no grupo do ticket (sala de chat)
        // =====================================================================

        // Cada ticket tem seu próprio "grupo" no SignalR
        public async Task EntrarNoTicket(int ticketId)
        {
            string grupo = $"ticket_{ticketId}";

            // Adiciona o cliente no grupo para receber mensagens desse ticket
            await Groups.AddToGroupAsync(Context.ConnectionId, grupo);

            Console.WriteLine($"🟢 {Context.ConnectionId} entrou no grupo {grupo}");
        }

        // =====================================================================
        // 💬 Enviar mensagem de texto (Cliente ↔ Técnico)
        // =====================================================================

        public async Task EnviarMensagem(int ticketId, string autor, string conteudo, string papel)
        {
            // Se o conteúdo estiver vazio, ignorar
            if (string.IsNullOrWhiteSpace(conteudo))
                return;

            string grupo = $"ticket_{ticketId}";

            try
            {
                // Carrega ticket com dados do cliente e técnico
                var ticket = await _context.Tickets
                    .Include(t => t.Criador)
                    .Include(t => t.Tecnico)
                    .FirstOrDefaultAsync(t => t.Id == ticketId);

                if (ticket == null)
                {
                    Console.WriteLine("❌ Ticket não encontrado!");
                    return;
                }

                // Obtém IDs
                int clienteId = ticket.Criador?.Id ?? 0;
                int tecnicoId = ticket.Tecnico?.Id ?? 0;

                // Nome de quem enviou a mensagem
                string remetenteNome = autor;

                // Define para quem a mensagem é destinada
                string destinatarioNome = papel.ToLower() == "cliente"
                    ? ticket.Tecnico?.Nome ?? "Técnico"
                    : ticket.Criador?.Username ?? "Cliente";

                // Cria nova mensagem para salvar no banco
                var novaMensagem = new Mensagem
                {
                    Remetente = remetenteNome,
                    Destinatario = destinatarioNome,
                    Conteudo = conteudo,
                    DataEnvio = DateTime.Now,
                    TicketId = ticketId,
                    ClienteId = clienteId,
                    TecnicoId = tecnicoId
                };

                // Grava no banco
                _context.Mensagens.Add(novaMensagem);
                await _context.SaveChangesAsync();

                // Envia mensagem para todos os usuários do grupo
                await Clients.Group(grupo).SendAsync("ReceberMensagem", new
                {
                    autor = remetenteNome,
                    mensagem = conteudo,
                    papel = papel,
                    data = novaMensagem.DataEnvio.ToString("yyyy-MM-dd HH:mm:ss")
                });

                Console.WriteLine($"📨 Mensagem salva e enviada no ticket {ticketId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro ao enviar mensagem: {ex.Message}");
            }
        }

        // =====================================================================
        // 📎 Enviar mensagem contendo arquivo (imagem ou arquivo geral)
        // =====================================================================

        public async Task EnviarArquivoMensagem(int ticketId, string autor, string urlArquivo, string papel)
        {
            if (string.IsNullOrWhiteSpace(urlArquivo))
                return;

            string grupo = $"ticket_{ticketId}";

            try
            {
                // Busca ticket e usuários
                var ticket = await _context.Tickets
                    .Include(t => t.Criador)
                    .Include(t => t.Tecnico)
                    .FirstOrDefaultAsync(t => t.Id == ticketId);

                if (ticket == null)
                {
                    Console.WriteLine("❌ Ticket não encontrado!");
                    return;
                }

                int clienteId = ticket.Criador?.Id ?? 0;
                int tecnicoId = ticket.Tecnico?.Id ?? 0;

                string remetenteNome = autor;

                string destinatarioNome = papel.ToLower() == "cliente"
                    ? ticket.Tecnico?.Nome ?? "Técnico"
                    : ticket.Criador?.Username ?? "Cliente";

                // Detecta tipo do arquivo pela extensão
                string extensao = System.IO.Path.GetExtension(urlArquivo).ToLower();
                string mensagem;

                // Se for imagem, envia como preview
                if (extensao == ".png" || extensao == ".jpg" || extensao == ".jpeg" ||
                    extensao == ".gif" || extensao == ".bmp")
                {
                    mensagem = $"<img src=\"{urlArquivo}\" alt=\"Imagem enviada\" style=\"max-width: 250px; border-radius: 10px; margin-top: 5px;\" />";
                }
                else
                {
                    // Se for arquivo geral, envia link para download
                    mensagem = $"<a href=\"{urlArquivo}\" target=\"_blank\" download style=\"text-decoration:none;color:#007bff;font-weight:bold;\">📎 Baixar arquivo</a>";
                }

                // Salva arquivo no banco
                var novoArquivo = new ChatFile
                {
                    FileName = System.IO.Path.GetFileName(urlArquivo),
                    FileUrl = urlArquivo,
                    UploadedAt = DateTime.Now,
                    UploadedById = papel.ToLower() == "tecnico" ? tecnicoId : clienteId,
                    UploadedToId = papel.ToLower() == "tecnico" ? clienteId : tecnicoId,
                    UploadedByName = remetenteNome,
                    TicketId = ticketId
                };

                _context.ChatFiles.Add(novoArquivo);

                // Salva mensagem vinculada ao arquivo
                var novaMensagem = new Mensagem
                {
                    Remetente = remetenteNome,
                    Destinatario = destinatarioNome,
                    Conteudo = mensagem,
                    DataEnvio = DateTime.Now,
                    TicketId = ticketId,
                    ClienteId = clienteId,
                    TecnicoId = tecnicoId
                };

                _context.Mensagens.Add(novaMensagem);
                await _context.SaveChangesAsync();

                // Notifica usuários
                await Clients.Group(grupo).SendAsync("ReceberMensagem", new
                {
                    autor = remetenteNome,
                    mensagem,
                    papel,
                    data = novaMensagem.DataEnvio.ToString("yyyy-MM-dd HH:mm:ss")
                });

                Console.WriteLine($"📁 Arquivo ({extensao}) enviado corretamente!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERRO EnviarArquivoMensagem: {ex.Message}");
            }
        }

        // =====================================================================
        // 🚫 Técnico solicita encerramento do chat
        // =====================================================================

        public async Task NotificarFechamentoChat(int ticketId)
        {
            string grupo = $"ticket_{ticketId}";

            // Envia notificação automática do sistema
            await Clients.Group(grupo).SendAsync("ReceberMensagem", new
            {
                autor = "sistema",
                mensagem = "🔴 O técnico solicitou o encerramento do atendimento. Aguardando confirmação.",
                papel = "sistema",
                data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            // Envia evento específico para a UI
            await Clients.Group(grupo).SendAsync("ChatEncerradoPeloTecnico", ticketId);
        }

        // =====================================================================
        // ✅ Cliente confirma encerramento do chat
        // =====================================================================

        public async Task ClienteConfirmouEncerrar(int ticketId)
        {
            string grupo = $"ticket_{ticketId}";

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                // Atualiza status
                ticket.Status = "Finalizado";
                ticket.FimAtendimento = DateTime.Now;

                // Calcula tempo total
                if (ticket.InicioAtendimento.HasValue)
                    ticket.TempoAtendimento = ticket.FimAtendimento - ticket.InicioAtendimento;

                await _context.SaveChangesAsync();

                // Notifica todos
                await Clients.Group(grupo).SendAsync("ChatEncerrado", new
                {
                    ticketId = ticketId,
                    status = ticket.Status
                });

                Console.WriteLine($"✅ Ticket {ticketId} finalizado com sucesso e notificado via SignalR!");
            }
        }

        // =====================================================================
        //  Cliente recusou encerramento
        // =====================================================================

        public async Task ClienteRecusouEncerrar(int ticketId)
        {
            string grupo = $"ticket_{ticketId}";

            await Clients.Group(grupo).SendAsync("ReceberMensagem", new
            {
                autor = "sistema",
                mensagem = "⚠️ O cliente optou por continuar a conversa.",
                papel = "sistema",
                data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
    }
}
