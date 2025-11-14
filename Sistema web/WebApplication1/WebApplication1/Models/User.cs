using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// Representa um usuário do sistema.
    /// Contém informações essenciais para autenticação e identificação no sistema.
    /// </summary>
    public class User
    {
        // 🔹 Identificador único do usuário (chave primária no banco de dados)
        [Key]
        public int Id { get; set; }

        // 🔹 Nome completo do usuário
        // Campo obrigatório, limitado a 100 caracteres
        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        [Display(Name = "Nome Completo")]
        public string Username { get; set; } = string.Empty;

        // 🔹 Endereço de e-mail do usuário
        // Campo obrigatório e deve ser um e-mail válido
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        [StringLength(100, ErrorMessage = "O email deve ter no máximo 100 caracteres.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // 🔹 Senha de acesso do usuário
        // Campo obrigatório, com mínimo de 6 e máximo de 100 caracteres
        // DataType.Password faz o campo ser tratado como senha nos formulários
        [Required(ErrorMessage = "A senha é obrigatória.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        [Display(Name = "Senha")]
        public string Password { get; set; } = string.Empty;

        public static implicit operator User(string v)
        {
            throw new NotImplementedException();
        }
    }
}
