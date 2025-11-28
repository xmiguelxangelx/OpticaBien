using Microsoft.EntityFrameworkCore;

namespace Optica1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Aquí agregaremos luego las entidades:
        // public DbSet<Persona> Personas { get; set; }
        // public DbSet<Paciente> Pacientes { get; set; }
        // public DbSet<Producto> Productos { get; set; }
        // public DbSet<Cita> Citas { get; set; }
    }
}
