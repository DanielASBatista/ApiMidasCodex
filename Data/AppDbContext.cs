using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProjetoMidasAPI.Models;
using ProjetoMidasAPI.Models.Enuns;
using ProjetoMidasAPI_Final.Models;
using ProjetoMidasAPI_Final.Models.Enuns;
using ProjetoMidasAPI_Final.Utils;

namespace ProjetoMidasAPI.Data
{
    // Essa classe é o "coração" do EF(migração) Core:, pq conecta as Models ao banco
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Cada DbSet vira uma tabela no banco
        public DbSet<Projecao> Projecoes { get; set; } = null!;
        public DbSet<Lancamento> Lancamentos { get; set; } = null!;
        public DbSet<Emprestimo> Emprestimos {get; set; } = null!;
        public DbSet<Recorrencia> Recorrencias {get; set;} = null!;
        public DbSet<TipoRecorrencia> TipoRecorrencias {get; set;}
        public DbSet<Empresa> Empresas {get; set;} = null!;
        public DbSet<Responsavel> Responsaveis {get; set;} = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        
        // É aqui o OnModelCreating
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define nomes de tabelas para serem alimentadas e chamadas. Também defini o relacionamentos e pk e fk
            modelBuilder.Entity<Projecao>().ToTable("Projecoes");
            modelBuilder.Entity<Projecao>().HasKey(p => p.IdProjecao);
            
            modelBuilder.Entity<Lancamento>().ToTable("Lancamentos");
            modelBuilder.Entity<Lancamento>().HasKey(l => l.IdLancamento);
            
            modelBuilder.Entity<Emprestimo>().ToTable("Emprestimos");
            modelBuilder.Entity<Emprestimo>().HasKey(e => e.IdSimEmprestimo);

            modelBuilder.Entity<Recorrencia>().ToTable("Recorrencias");
            modelBuilder.Entity<Recorrencia>().HasKey(r => r.idRecorrente);

            modelBuilder.Entity<Empresa>().ToTable("Empresas");
            modelBuilder.Entity<Empresa>().HasKey(e => e.IdEmpresa);

            modelBuilder.Entity<Responsavel>().ToTable("Responsaveis"); 
            modelBuilder.Entity<Responsavel>().HasKey(r => r.IdResponsavel);

            modelBuilder.Entity<TipoRecorrencia>().ToTable("TipoRecorrencias");
            modelBuilder.Entity<TipoRecorrencia>().HasKey(tr => tr.Id);

            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);    
            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Lancamentos)
                .WithOne(l => l.Usuario)
                .HasForeignKey(l => l.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Projecoes)
                .WithOne(p => p.Usuario)
                .HasForeignKey(p => p.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Emprestimos)
                .WithOne(e => e.Usuario)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Recorrencias)
                .WithOne(r => r.Usuario)
                .HasForeignKey(r => r.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>().ToTable("Transactions");
            modelBuilder.Entity<Transaction>().HasKey(t => t.Id);
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Usuario)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Origin)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TipoRecorrencia>().HasData(
                new TipoRecorrencia { Id = 1, Nome = "Mensal", PadraoSistema = true },
                new TipoRecorrencia { Id = 2, Nome = "Semanal", PadraoSistema = true },
                new TipoRecorrencia { Id = 3, Nome = "Anual", PadraoSistema = true }
            );
            modelBuilder.Entity<Empresa>().HasData(
                new Empresa { IdEmpresa = 1, idResponsavel = 1, razaoSocial = "Empresa Teste", nomeFantasia = "Teste", telefoneEmp = "123456789", cnpjEmpresa = "12345678901234", emailEmpresa = "empresa@teste.com"}
            );
            modelBuilder.Entity<Responsavel>().HasData(
                new Responsavel { IdResponsavel = 1, nomeResponsavel = "João", sobrenomeResponsavel = "Silva", telefoneResponsavel = "987654321", emailResponsavel = "joao.silva@teste.com"}
            );
            Usuario user = new Usuario();
            Criptografia.CriarPasswordHash("Senha@123", out byte[] hash, out byte[] salt);
            user.IdUsuario = 1;
            user.nomeUsuario = "Admin";
            user.PasswordString = string.Empty;
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.Perfil = "Administrador";
            user.TipoUsuario = TipoUsuarioEnum.Administrador;
            
            modelBuilder.Entity<Usuario>().HasData(user);

            //Define que se o perfil não for informado, o valor padrão será "Visitante"
            modelBuilder.Entity<Usuario>().Property(u => u.Perfil).HasDefaultValue("Visitante");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings => warnings
                .Ignore(RelationalEventId.PendingModelChangesWarning));
        }
    }
}
