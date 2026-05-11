using System;

namespace Domain
{
#nullable enable
    public class Document
    {
        public Guid Id { get; set; }

        public string? FileText { get; set; }

        public string? FileName { get; set; }

        public DocumentFileType FileType { get; set; }

        public long FileSize { get; set; }

        public DateTimeOffset RecievedAt { get; set; }

        public DocumentProcessingStatus ProcessingStatus { get; set; }

        public DateTimeOffset? ProcessedAt { get; set; }
    }
}