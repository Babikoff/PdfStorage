using Domain;
using System;

namespace DTO.Queue
{
    public record QueueDocumentDto
    {
        public Guid Id { get; init; }

        public byte[] RawFileData { get; init; } = default!;

        public string FileName { get; init; }

        public DocumentFileType FileType { get; init; }

        public long? FileSize { get; init; }

        public DateTimeOffset RecievedAt { get; init; }

        public DocumentProcessingStatus ProcessingStatus { get; init; }
    }
}
