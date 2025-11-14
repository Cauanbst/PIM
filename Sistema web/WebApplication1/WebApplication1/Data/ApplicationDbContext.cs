using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    /// <summary>
    /// 🧩 ApplicationDbContext — Classe principal do Entity Framework Core.
    /// É responsável por mapear todas as tabelas e configurar os relacionamentos.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        // ============================================================
        // 🔹 Construtor: recebe as opções do contexto (conexão, etc.)
        // ============================================================
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ============================================================
        // 🔹 Definição das tabelas que o EF Core vai gerenciar
        // ============================================================
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Technician> TbTecnico { get; set; } = null!;  
        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<ChatFile> ChatFiles { get; set; } = null!;
        public DbSet<Mensagem> Mensagens { get; set; } = null!;

        // ============================================================
        // 🔧 Método de configuração do modelo (mapeamento e regras)
        // ============================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ------------------------------------------------------------
            // 🧱 TABELA: tb_usuario — representa os clientes
            // ------------------------------------------------------------
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("tb_usuario");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Username)
                      .HasColumnName("nome")
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Email)
                      .HasColumnName("email")
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Password)
                      .HasColumnName("senha")
                      .IsRequired()
                      .HasMaxLength(100);
            });

            // ------------------------------------------------------------
            // 🧱 TABELA: tb_tecnico — técnicos de suporte
            // ------------------------------------------------------------
            modelBuilder.Entity<Technician>(entity =>
            {
                entity.ToTable("tb_tecnico");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nome)
                      .HasColumnName("nome")
                      .IsRequired()
                      .HasMaxLength(100);

              

                entity.Property(e => e.Email)
                      .HasColumnName("email")
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Senha)
                      .HasColumnName("senha")
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Especialidade)
                      .HasColumnName("especialidade")
                      .IsRequired()
                      .HasMaxLength(50);
            });

            // ------------------------------------------------------------
            // 🧱 TABELA: Tickets — chamados abertos pelos usuários
            // ------------------------------------------------------------
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.ToTable("Tickets");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.Description)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(e => e.Status)
                      .HasDefaultValue("Aberto")
                      .HasMaxLength(50);

                entity.Property(e => e.DataCriacao)
                      .IsRequired();

                entity.Property(e => e.InicioAtendimento);
                entity.Property(e => e.FimAtendimento);

                entity.Property(e => e.TempoAtendimento)
                      .HasConversion(
                          v => v.HasValue ? v.Value.ToString() : null,
                          v => !string.IsNullOrEmpty(v) ? TimeSpan.Parse(v) : (TimeSpan?)null
                      )
                      .HasMaxLength(50);

                entity.HasOne(u => u.Criador)
                      .WithMany()
                      .HasForeignKey(t => t.CriadorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Tecnico)
                      .WithMany()
                      .HasForeignKey(t => t.TecnicoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // 🧱 TABELA: ChatFiles — arquivos enviados no chat
            // ------------------------------------------------------------
            modelBuilder.Entity<ChatFile>(entity =>
            {
                entity.ToTable("ChatFiles");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FileName)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.FileUrl)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(e => e.UploadedAt)
                      .IsRequired();

                entity.Property(e => e.UploadedByName)
                      .HasMaxLength(150)
                      .IsRequired(false);

                entity.Property(e => e.UploadedById)
                      .IsRequired(false);

                entity.Property(e => e.UploadedByTecnicoId)
                      .IsRequired(false);

                entity.Property(e => e.UploadedToId)
                      .IsRequired();

                entity.Property(e => e.TicketId)
                      .IsRequired();

                entity.HasOne<Ticket>()
                      .WithMany()
                      .HasForeignKey(e => e.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TicketId);
                entity.HasIndex(e => e.UploadedById);
                entity.HasIndex(e => e.UploadedByTecnicoId);
                entity.HasIndex(e => e.UploadedToId);
            });

            // ------------------------------------------------------------
            // 🧱 TABELA: Mensagens — mensagens do chat
            // ------------------------------------------------------------
            modelBuilder.Entity<Mensagem>(entity =>
            {
                entity.ToTable("Mensagens");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Remetente)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Destinatario)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Conteudo)
                      .IsRequired();

                entity.Property(e => e.DataEnvio)
                      .IsRequired();

                entity.Property(e => e.TicketId)
                      .IsRequired();

                entity.HasOne(e => e.Ticket)
                      .WithMany()
                      .HasForeignKey(e => e.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
