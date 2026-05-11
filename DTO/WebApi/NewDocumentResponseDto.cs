using Domain;
using System;
using System.ComponentModel.DataAnnotations;

namespace DTO.WebApi
{
    public record NewDocumentResponseDto
    {
        public Guid Id { get; set; }

        public string FileName { get; set; }

        public DocumentFileType FileType { get; set; }

        public long? FileSize { get; set; }


        public DateTimeOffset RecievedAt { get; set; }

        public DocumentProcessingStatus ProcessingStatus { get; set; }
    }
}
