using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    /// <summary>
    /// 📝 Classe que representa um ticket de suporte no sistema.
    /// Permite armazenar informações do ticket, datas, status e duração do atendimento.
    /// </summary>
    public class Ticket
    {
        // 🔹 Identificador único do ticket (PK)
        [Key]
        public int Id { get; set; }

        // 🔹 Título do ticket, obrigatório, com tamanho máximo de 100 caracteres
        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        // 🔹 Descrição detalhada do ticket, obrigatório, até 1000 caracteres
        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        // 🔹 Status atual do ticket, obrigatório, padrão "Aberto"
        [Required, StringLength(50)]
        public string Status { get; set; } = "Aberto";

        // 🔹 Data e hora em que o ticket foi criado, obrigatório, padrão agora
        [Required]
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        // 🔹 Data e hora de início do atendimento (pode ser nulo se não iniciado)
        public DateTime? InicioAtendimento { get; set; }

        // 🔹 Data e hora de fim do atendimento (pode ser nulo se não finalizado)
        public DateTime? FimAtendimento { get; set; }

        // 🔹 Duração do atendimento, calculada como diferença entre início e fim (pode ser nulo)
        // Armazenada como VARCHAR no banco via ValueConverter
        public TimeSpan? TempoAtendimento { get; set; }

        // 👤 Relação com o usuário que criou o ticket (chave estrangeira)
        [ForeignKey("Criador")]
        public int CriadorId { get; set; }
        public User Criador { get; set; } = null!; // Navegação para o objeto usuário

        // 🔧 Relação com o técnico responsável pelo ticket (chave estrangeira)
        [ForeignKey("Tecnico")]
        public int? TecnicoId { get; set; }
        public Technician? Tecnico { get; set; }  // Navegação para o objeto técnico
    }
}
