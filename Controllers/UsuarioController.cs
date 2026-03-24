using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoMidasAPI.Data;
using ProjetoMidasAPI_Final.Utils;
using ProjetoMidasAPI.Dtos;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

namespace ProjetoMidasAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/[controller]")]
    
    public class UsuarioController : ControllerBase
    {
        private readonly AppDbContext _context;

        private readonly IConfiguration _configuration;

        public UsuarioController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        private async Task<bool> UsuarioExistente(string nomeUsuario)
        {
            if (await _context.Usuarios.AnyAsync(u => u.nomeUsuario.ToLower() == nomeUsuario.ToLower()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private string CriarToken(Usuario usuario)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.nomeUsuario)
            };

            //Pra não virar bagunça e ficar citando a instancia a toda hora nós só definimos uma variável pra ela e chamamos como sendo uma nova instancia.
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("ConfiguracaoToken:Chave").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(29),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
        [HttpGet("ValidarToken")]
        public IActionResult ValidarToken()
        {
            return Ok(new { valido = true });
        }
        
        [AllowAnonymous]
        [HttpPost("Registrar")]
        public async Task<IActionResult> RegistrarUsuario(RegistrarUsuarioDto dto)
        {
            if (await UsuarioExistente(dto.NomeUsuario))
                return BadRequest("Nome de usuário já existe!");

            Usuario usuario = new Usuario
            {
                nomeUsuario = dto.NomeUsuario
            };

            Criptografia.CriarPasswordHash(dto.PasswordString, out byte[] hash, out byte[] salt);

            usuario.PasswordHash = hash;
            usuario.PasswordSalt = salt;

            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();

            return Ok(new { mensagem = "Usuário registrado com sucesso!" });
        }
        [AllowAnonymous]
        [HttpPost("Autenticar")]
        public async Task<IActionResult> AutenticarUsuario(Usuario credenciais)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => 
                        u.nomeUsuario.ToLower() == credenciais.nomeUsuario.ToLower());

                if (usuario == null)
                    return Unauthorized("Usuário não encontrado.");

                if (!Criptografia.VerificarPasswordHash(
                    credenciais.PasswordString,
                    usuario.PasswordHash!,
                    usuario.PasswordSalt!))
                {
                    return Unauthorized("Senha incorreta.");
                }

                var token = CriarToken(usuario);

                var response = new LoginResponseDto
                {
                    Token = token,
                    Usuario = new UsuarioDto
                    {
                        Id = usuario.IdUsuario,
                        NomeUsuario = usuario.nomeUsuario
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao autenticar usuário: {ex.Message}");
            }
        }
        [HttpPut("AlterarSenha")]
        public async Task<IActionResult> AlterarSenhaUsuario(Usuario credenciais)
        {
            try
            {
                Usuario? usuario = await _context.Usuarios //Busca o usuário no banco através do login
                .FirstOrDefaultAsync(U => U.nomeUsuario.ToLower().Equals(credenciais.nomeUsuario.ToLower()));

                if (usuario == null) // Se o usuário não for encontrado, lança uma exceção
                    throw new System.Exception("Usuário não encontrado.");
                    Criptografia.CriarPasswordHash(credenciais.PasswordString, out byte[] hash, out byte[] salt);
                    usuario.PasswordHash = hash; //Se o usuário existir, executa a criptografia
                    usuario.PasswordSalt = salt; //Atualiza o hash e o salt no banco
                        
                    _context.Usuarios.Update(usuario);
                    int linhasAfetadas = await _context.SaveChangesAsync(); // Cponfirma a alteração no banco
                    return Ok(linhasAfetadas); //Retorna o número de linhas afetadas (Geralmente sempre tem 1 linha msm)
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Erro ao alterar a senha do usuário: {ex.Message}");
            }
        }
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _context.Set<Usuario>().ToListAsync();
        }
        [HttpGet("Get/{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
            .Include(u => u.Transactions)
            .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }
        [HttpPost("New")]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            _context.Set<Usuario>().Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsuario", new { id = usuario.IdUsuario }, usuario);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Set<Usuario>().FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Set<Usuario>().Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
