using Domain;
using System;
using System.ComponentModel.DataAnnotations;

namespace DTO.WebApi
{
    public class NewDocumentResponseDto
    {
        public Guid Id { get; set; }

        public string FileName { get; set; }

        public DocumentFileType FileType { get; set; }

        public long? FileSize { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
