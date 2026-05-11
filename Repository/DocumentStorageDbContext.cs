using Domain;
using Microsoft.EntityFrameworkCore;

namespace Repository
{
    public class DocumentStorageDbContext : DbContext
    {
        public DocumentStorageDbContext(DbContextOptions<DocumentStorageDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new DocumentConfiguration());
        }
    }
}