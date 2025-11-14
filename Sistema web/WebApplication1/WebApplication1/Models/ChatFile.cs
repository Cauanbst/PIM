using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    /// <summary>
    /// 📁 Representa um arquivo (imagem, documento, etc.) enviado no chat.
    /// </summary>
    public class ChatFile
    {
        [Key]
        public int Id { get; set; }

        // 🔹 Nome do arquivo enviado (ex: "foto.jpg")
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        // 🔹 Caminho ou URL onde o arquivo foi salvo no servidor
        [Required]
        [MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        // 🔹 Data e hora do envio
        [Required]
        public DateTime UploadedAt { get; set; }

        // 🔹 ID de quem enviou o arquivo (cliente)
        public int? UploadedById { get; set; }

        // 🔹 ID do técnico que enviou o arquivo (opcional)
        public int? UploadedByTecnicoId { get; set; }

        // 🔹 ID de quem recebeu o arquivo (cliente ou técnico)
        public int UploadedToId { get; set; }

        // 🔹 ID do ticket (chamado) relacionado ao arquivo
        public int TicketId { get; set; }

        // 🔹 Nome de quem enviou (cliente ou técnico)
        [MaxLength(100)]
        public string? UploadedByName { get; set; }
    }
}
