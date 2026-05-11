using AutoMapper;
using Domain;
using DTO.Queue;
using DTO.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MappingProfiles.Tests
{
    /// <summary>
    /// Тесты для проверки корректности конфигурации AutoMapper.
    /// </summary>
    public class MappingProfileTests
    {
        private readonly IMapper _mapper;

        public MappingProfileTests()
        {
            var config = new MapperConfiguration(
                cfg => { cfg.AddProfile<MappingProfile>(); },
                loggerFactory: NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void AutoMapper_Configuration_IsValid()
        {
            // Act & Assert — проверяем, что все маппинги корректно сконфигурированы
            _mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }

        [Fact]
        public void QueueDocumentDto_To_Document_MapsCorrectly()
        {
            // Arrange
            var dto = new QueueDocumentDto
            {
                Id = Guid.NewGuid(),
                FileName = "test.pdf",
                FileType = DocumentFileType.Pdf,
                FileSize = 100,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.InQueue,
                RawFileData = [0x25, 0x50, 0x44, 0x46]
            };

            // Act
            var document = _mapper.Map<Document>(dto);

            // Assert
            Assert.NotNull(document);
            Assert.Equal(dto.Id, document.Id);
            Assert.Equal(dto.FileName, document.FileName);
            Assert.Equal(dto.FileType, document.FileType);
            Assert.Equal(dto.FileSize, document.FileSize);
            Assert.Equal(dto.RecievedAt, document.RecievedAt);
            Assert.Equal(dto.ProcessingStatus, document.ProcessingStatus);
        }

        [Fact]
        public void QueueDocumentDto_To_NewDocumentResponseDto_MapsCorrectly()
        {
            // Arrange
            var dto = new QueueDocumentDto
            {
                Id = Guid.NewGuid(),
                FileName = "response.pdf",
                FileType = DocumentFileType.Pdf,
                FileSize = 200,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.InQueue
            };

            // Act
            var response = _mapper.Map<NewDocumentResponseDto>(dto);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(dto.Id, response.Id);
            Assert.Equal(dto.FileName, response.FileName);
            Assert.Equal(dto.FileType, response.FileType);
            Assert.Equal(dto.FileSize, response.FileSize);
            Assert.Equal(dto.RecievedAt, response.RecievedAt);
            Assert.Equal(dto.ProcessingStatus, response.ProcessingStatus);
        }

        [Fact]
        public void Document_To_QueueDocumentDto_MapsCorrectly()
        {
            // Arrange
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = "doc.pdf",
                FileType = DocumentFileType.Pdf,
                FileSize = 300,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.Processed,
                ProcessedAt = DateTimeOffset.UtcNow,
                FileText = "текст"
            };

            // Act
            var dto = _mapper.Map<QueueDocumentDto>(document);

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(document.Id, dto.Id);
            Assert.Equal(document.FileName, dto.FileName);
            Assert.Equal(document.FileType, dto.FileType);
            Assert.Equal(document.FileSize, dto.FileSize);
            Assert.Equal(document.RecievedAt, dto.RecievedAt);
            Assert.Equal(document.ProcessingStatus, dto.ProcessingStatus);
        }

        [Fact]
        public void Document_To_DocumentListItemDto_MapsCorrectly()
        {
            // Arrange
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = "list-item.pdf",
                FileType = DocumentFileType.Pdf,
                FileSize = 150,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.Processed,
                ProcessedAt = DateTimeOffset.UtcNow
            };

            // Act
            var listItem = _mapper.Map<DocumentListItemDto>(document);

            // Assert
            Assert.NotNull(listItem);
            Assert.Equal(document.Id, listItem.Id);
            Assert.Equal(document.FileName, listItem.FileName);
            Assert.Equal(document.FileType, listItem.FileType);
            Assert.Equal(document.FileSize, listItem.FileSize);
            Assert.Equal(document.RecievedAt, listItem.RecievedAt);
            Assert.Equal(document.ProcessingStatus, listItem.ProcessingStatus);
        }

        [Fact]
        public void Document_WithNullFields_MapsToNullables()
        {
            // Arrange
            var document = new Document
            {
                Id = Guid.NewGuid(),
                FileName = null,
                FileType = DocumentFileType.Unknown,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.Unknown
            };

            // Act — не должно выбросить исключение
            var dto = _mapper.Map<QueueDocumentDto>(document);
            var listItem = _mapper.Map<DocumentListItemDto>(document);

            // Assert
            Assert.NotNull(dto);
            Assert.NotNull(listItem);
        }
    }
}