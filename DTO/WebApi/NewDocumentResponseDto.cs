using Domain;
using System;
using System.ComponentModel.DataAnnotations;

namespace DTO.WebApi
{
    public record NewDocumentResponseDto
    {
        public Guid Id { get; init; }

        public string FileName { get; init; }

        public DocumentFileType FileType { get; init; }

        public long? FileSize { get; init; }


        public DateTimeOffset RecievedAt { get; init; }

        public DocumentProcessingStatus ProcessingStatus { get; init; }
    }
}
