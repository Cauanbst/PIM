// Importa funcionalidades de validação de dados (DataAnnotations)
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    // Declara a classe Technician, que representa um técnico do sistema
    public class Technician
    {
        // Indica que essa propriedade é a chave primária no banco
        [Key]
        public int Id { get; set; }

        // Campo obrigatório – não pode ser vazio
        // Mensagem exibida caso o campo não seja preenchido
        [Required(ErrorMessage = "O nome é obrigatório.")]
        // Limita o tamanho do nome a 100 caracteres
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        // Define o nome amigável que aparece nos formulários
        [Display(Name = "Nome Completo")]
        // Variável que guarda o nome do técnico
        public string Nome { get; set; } = string.Empty;

        // Campo obrigatório para armazenar o e-mail do técnico
        [Required(ErrorMessage = "O email é obrigatório.")]
        // Valida se o formato do e-mail é correto
        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        // Define limite máximo de caracteres
        [StringLength(100, ErrorMessage = "O email deve ter no máximo 100 caracteres.")]
        // Nome amigável exibido nos formulários
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        // Senha obrigatória
        [Required(ErrorMessage = "A senha é obrigatória.")]
        // Tamanho mínimo e máximo permitido da senha
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        // Exibe a senha como campo do tipo "password" (oculto)
        [DataType(DataType.Password)]
        // Nome exibido nos formulários
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        // Especialidade do técnico é obrigatória
        [Required(ErrorMessage = "A especialidade é obrigatória.")]
        // Limita o tamanho da especialidade
        [StringLength(50, ErrorMessage = "A especialidade deve ter no máximo 50 caracteres.")]
        // Nome exibido no formulário
        [Display(Name = "Especialidade")]
        // Campo que guarda a área de especialidade do técnico
        public string Especialidade { get; set; } = string.Empty;
    }
}
