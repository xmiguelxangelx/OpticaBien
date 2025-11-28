using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Optica1.Data;

namespace Optica1.Data
{
    // Esta fábrica SOLO se usa en tiempo de diseño (migraciones)
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // 🔐 Cadena de conexión DIRECTA para las migraciones
            var connectionString = "server=127.0.0.1;port=3306;database=OpticaLinconsDb;user=root;password=1234;";

            var serverVersion = new MySqlServerVersion(new Version(8, 0, 36)); // Ajusta versión si quieres

            optionsBuilder.UseMySql(connectionString, serverVersion);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
