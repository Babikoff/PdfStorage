using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repository
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("documents", t =>
            {
                t.HasCheckConstraint(
                    "ck_documents_data_max_10mb",
                    "octet_length(data) <= 10 * 1024 * 1024");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

            builder.Property(x => x.Data)
                .HasColumnName("data")
                .HasColumnType("bytea")
                .IsRequired();

            builder.Property(x => x.FileType)
                .HasColumnName("file_type")
                .HasColumnType("smallint")
                .IsRequired();

            builder.Property(x => x.FileSize)
                .HasColumnName("file_size")
                .HasColumnType("bigint");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestampz")
                .HasDefaultValue("now()");
        }
    }
}
