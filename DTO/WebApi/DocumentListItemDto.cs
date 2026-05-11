using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.WebApi
{
    public record DocumentListItemDto
    {
        public Guid Id { get; init; }

        public string FileName { get; init; }

        public DocumentFileType FileType { get; init; }

        public long? FileSize { get; set; }

        public DateTimeOffset RecievedAt { get; init; }

        public DocumentProcessingStatus ProcessingStatus { get; init; }

        public DateTimeOffset ProcessedAt { get; init; }
    }
}
