using Common;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("documents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

            builder.Property(x => x.FileText)
                .HasColumnName("file_text")
                .HasColumnType("text");

            builder.Property(x => x.FileName)
                .HasColumnName("file_name")
                .HasColumnType("text");

            builder.Property(x => x.FileType)
                .HasColumnName("file_type")
                .HasColumnType("smallint")
                .IsRequired();

            builder.Property(x => x.FileSize)
                .HasColumnName("file_size")
                .HasColumnType("bigint");

            builder.Property(x => x.RecievedAt)
                .HasColumnName("recieved_at")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("now()");

            builder.Property(x => x.ProcessingStatus)
                .HasColumnName("processing_status")
                .HasColumnType("smallint")
                .IsRequired();

            builder.Property(x => x.ProcessedAt)
                .HasColumnName("processed_at")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("null");
        }
    }
}