using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    /// <summary>
    /// Representa uma mensagem trocada entre cliente e técnico no contexto de um ticket.
    /// </summary>
    [Table("Mensagens")] // Nome da tabela no banco de dados
    public class Mensagem
    {
        // 🔹 Chave primária da mensagem
        [Key]
        public int Id { get; set; }

        // 🔹 Nome do remetente (exemplo: "cliente", "técnico")
        [Required]
        [StringLength(100)]
        public string Remetente { get; set; } = string.Empty;

        // 🔹 Nome do destinatário
        [Required]
        [StringLength(100)]
        public string Destinatario { get; set; } = string.Empty;

        // 🔹 Conteúdo da mensagem
        [Required]
        public string Conteudo { get; set; } = string.Empty;

        // 🔹 Data e hora em que a mensagem foi enviada
        [Required]
        public DateTime DataEnvio { get; set; } = DateTime.Now;

        // 🔹 Relacionamento com o ticket
        [Required]
        public int TicketId { get; set; }

        [ForeignKey("TicketId")]
        public Ticket? Ticket { get; set; }

        // 🔹 FK para o cliente (opcional, pois nem toda mensagem é dele)
        public int? ClienteId { get; set; }

        // Se tiver tabela de clientes, pode fazer a navegação:
        // [ForeignKey("ClienteId")]
        // public Cliente? Cliente { get; set; }

        // 🔹 FK para o técnico (opcional)
        public int? TecnicoId { get; set; }

        // Se tiver tabela de técnicos, pode fazer a navegação:
        // [ForeignKey("TecnicoId")]
        // public Tecnico? Tecnico { get; set; }
    }
}
