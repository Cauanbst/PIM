using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Hubs;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class UploadController : Controller
    {
        private readonly IWebHostEnvironment _env;          // Ambiente da aplicação (para pegar wwwroot)
        private readonly ApplicationDbContext _dbContext;    // Banco de dados
        private readonly IHubContext<ChatHub> _hubContext;   // Acesso ao SignalR

        // Extensões de arquivo permitidas
        private readonly string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt" };

        // Tamanho máximo permitido (10MB)
        private const long maxFileSize = 10 * 1024 * 1024;

        public UploadController(
            IWebHostEnvironment env,
            ApplicationDbContext dbContext,
            IHubContext<ChatHub> hubContext)
        {
            _env = env;
            _dbContext = dbContext;
            _hubContext = hubContext;
        }

        // ===========================================================
        // Upload de arquivos + notificação via SignalR
        // ===========================================================
        [HttpPost]
        public async Task<IActionResult> Create(IFormFile file, int ticketId, string usuario)
        {
            try
            {
                // Log de debug para saber o que foi recebido
                Console.WriteLine($"[UPLOAD DEBUG] Recebido: {file?.FileName}, Extensão: {Path.GetExtension(file?.FileName)}, Tamanho: {file?.Length}, ticketId: {ticketId}, usuario: {usuario}");

                // Verifica se o arquivo foi realmente enviado
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "Nenhum arquivo foi selecionado." });

                // Verifica extensão permitida
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { error = "Tipo de arquivo não permitido." });

                // Verifica tamanho
                if (file.Length > maxFileSize)
                    return BadRequest(new { error = $"O arquivo não pode exceder {maxFileSize / (1024 * 1024)} MB." });

                // Cria pasta uploads caso não exista
                var uploadPath = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                // Gera nome único para não sobrescrever outro arquivo
                var uniqueName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadPath, uniqueName);

                // Salva arquivo no servidor
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                // URL de acesso público ao arquivo
                var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{uniqueName}";
                Console.WriteLine($"[UPLOAD URL] {fileUrl}");

                // Carrega dados do ticket
                var ticket = await _dbContext.Tickets
                    .Include(t => t.Criador)
                    .Include(t => t.Tecnico)
                    .FirstOrDefaultAsync(t => t.Id == ticketId);

                if (ticket == null)
                    return BadRequest(new { error = "Ticket não encontrado." });

                int uploadedById = 0, uploadedToId = 0;
                string uploadedByName = "Desconhecido";
                bool isTecnico = false;

                // ===========================================================
                // Identifica se quem enviou é técnico ou cliente
                // ===========================================================

                // Tenta localizar técnico pelo nome ou e-mail
                var tecnico = await _dbContext.TbTecnico
                    .FirstOrDefaultAsync(t => t.Nome == usuario || t.Email == usuario);

                if (tecnico != null)
                {
                    // Quem enviou é TÉCNICO
                    isTecnico = true;

                    uploadedById = tecnico.Id;
                    uploadedToId = ticket.Criador?.Id ?? 0;
                    uploadedByName = tecnico.Nome ?? "Técnico";

                    Console.WriteLine($"[UPLOAD] Técnico detectado: {uploadedByName} (ID: {uploadedById})");
                }
                else
                {
                    // Caso contrário, é CLIENTE
                    var cliente = await _dbContext.Users
                        .FirstOrDefaultAsync(u => u.Username == usuario || u.Email == usuario);

                    uploadedById = cliente?.Id ?? ticket.Criador?.Id ?? 0;
                    uploadedToId = ticket.Tecnico?.Id ?? 0;
                    uploadedByName = cliente?.Username ?? ticket.Criador?.Username ?? "Cliente";

                    Console.WriteLine($"[UPLOAD] Cliente detectado: {uploadedByName} (ID: {uploadedById})");
                }

                // Se, por algum motivo, não encontrou ninguém
                if (uploadedById == 0)
                {
                    Console.WriteLine("[UPLOAD ERROR] Nenhum ID válido encontrado para o remetente.");
                    return BadRequest(new { error = "Usuário não encontrado no sistema." });
                }

                // ===========================================================
                // Salva o registro do arquivo no banco
                // ===========================================================
                var chatFile = new ChatFile
                {
                    FileName = file.FileName,
                    FileUrl = fileUrl,
                    UploadedAt = DateTime.Now,
                    UploadedToId = uploadedToId,
                    UploadedByName = uploadedByName,
                    TicketId = ticketId,
                    UploadedById = isTecnico ? null : uploadedById,   // Se for técnico → null
                    UploadedByTecnicoId = isTecnico ? uploadedById : null
                };

                _dbContext.ChatFiles.Add(chatFile);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"[UPLOAD OK ✅] {file.FileName} salvo no servidor para ticket {ticketId}.");

                // ===========================================================
                // Envia notificação para o SignalR (chat)
                // Prefixo "file:" evita duplicação de conteúdo
                // ===========================================================
                await _hubContext.Clients.Group($"ticket_{ticketId}")
                    .SendAsync("ReceberMensagem", new
                    {
                        autor = uploadedByName,
                        mensagem = $"file:{fileUrl}",        // FRONT sabe que é arquivo
                        nomeOriginal = file.FileName,
                        data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        papel = isTecnico ? "tecnico" : "cliente"
                    });

                Console.WriteLine($"[SIGNALR ✅] Arquivo enviado via SignalR para ticket {ticketId}.");

                // Retorna URL do arquivo ao frontend
                return Json(new { success = true, fileUrl });
            }
            catch (Exception ex)
            {
                // Log em caso de erro inesperado
                Console.WriteLine($"[UPLOAD ERROR ❌] {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = "Erro interno ao enviar o arquivo: " + ex.Message });
            }
        }
    }
}
