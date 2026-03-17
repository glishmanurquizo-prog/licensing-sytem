using Microsoft.EntityFrameworkCore;
using LicensingServer.Models;

namespace LicensingServer.Data
{
    // Clase DbContext actualizada y segura para .NET 10/8
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<License> Licenses { get; set; }
        public DbSet<Activation> Activations { get; set; }

        // Si necesitas configurar nombres de tablas u opciones adicionales:
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<License>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.LicenseKey).IsRequired();
                b.HasIndex(x => x.LicenseKey).IsUnique();
            });

            modelBuilder.Entity<Activation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.HwidHash).IsRequired();
                b.HasIndex(x => new { x.LicenseId, x.HwidHash });
            });
        }
    }
}