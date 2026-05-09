using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain
{
#nullable enable
    public class Document
    {
        public Guid Id { get; set; }

        [Required]
        public byte[] Data { get; set; } = default!;

        public string? FileName { get; set; }

        public DocumentFileType FileType { get; set; }

        public long? FileSize { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
