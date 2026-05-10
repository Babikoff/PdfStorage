using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.WebApi
{
    public record DocumentListItemDto
    {
        public Guid Id { get; set; }

        public byte[] Data { get; set; }

        public string FileName { get; set; }

        public DocumentFileType FileType { get; set; }

        public long? FileSize { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
