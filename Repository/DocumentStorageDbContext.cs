using Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repository
{
    public class DocumentStorageDbContext : DbContext
    {
        public DocumentStorageDbContext(DbContextOptions<DocumentStorageDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }
    }
}
