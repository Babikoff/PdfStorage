using Domain;
using System;
using System.ComponentModel.DataAnnotations;

namespace DTO.Queue
{
    public record QueueDocumentDto
    {
        public Guid Id { get; set; }

        public byte[] Data { get; set; } = default!;

        public string FileName { get; set; }

        public DocumentFileType FileType { get; set; }

        public long? FileSize { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
