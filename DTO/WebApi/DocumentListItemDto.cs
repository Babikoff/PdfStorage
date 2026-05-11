using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.WebApi
{
    public record DocumentListItemDto
    {
        public Guid Id { get; set; }

        public string FileName { get; set; }

        public DocumentFileType FileType { get; set; }

        public long? FileSize { get; set; }

        public DateTimeOffset RecievedAt { get; set; }

        public DocumentProcessingStatus ProcessingStatus { get; set; }

        public DateTimeOffset ProcessedAt { get; set; }
    }
}
