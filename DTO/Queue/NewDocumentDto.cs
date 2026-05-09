using Domain;
using System;
using System.ComponentModel.DataAnnotations;

namespace DTO.Queue
{
    public class NewDocumentDto
    {
        public Guid Id { get; set; }

        public string FileName { get; set; }

        public DocumentFileType FileType { get; set; }

        public long? FileSize { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
