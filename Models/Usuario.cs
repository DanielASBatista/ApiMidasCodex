using ProjetoMidasAPI.Models;
using ProjetoMidasAPI_Final.Models.Enuns;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Usuario
{
    [Key] // Define a chave primária da tabela
    public int IdUsuario { get; set; }

    [Required, MaxLength(50)] // Nome de usuário. Campo obrigatório, máximo 50 caracteres.
    public string nomeUsuario { get; set; } = string.Empty;

    public byte[]? PasswordHash { get; set; }
    public byte[]? PasswordSalt { get; set; }

    [NotMapped]
    public string PasswordString { get; set; } = string.Empty;

    public List<Lancamento> Lancamentos { get; set; } = new();
    public List<Projecao> Projecoes { get; set; } = new();
    public List<Emprestimo> Emprestimos { get; set; } = new();
    public List<Recorrencia> Recorrencias { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();

    [Required] // Id da empresa associada ao usuário. Campo obrigatório.
    public int IdEmpresa { get; set; } 
    //[Required] No banco de dados ele ta como Not Null mas não sei como colocar ele no sistema ainda. Vou arrumar.
    
    public TipoUsuarioEnum TipoUsuario { get; set; }

    public string? Perfil { get; set; }
    
    [MaxLength(50)] // Sobrenome do usuário. Campo obrigatório, máximo 50 caracteres.
    public string sobrenome { get; set; } = string.Empty;
    
    [MaxLength(100)] // Email do usuário. Campo obrigatório, máximo 100 caracteres.
    public string emailUsuario { get; set; } = string.Empty;
    
    [MaxLength(11)] // Telefone do usuário. Campo obrigatório, máximo 11 caracteres.
    public string telefone { get; set; } = string.Empty;

    [NotMapped]
    public string Token { get; set; } = string.Empty;




}
