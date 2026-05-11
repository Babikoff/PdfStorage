using AutoMapper;
using Contracts;
using Domain;
using DTO.Queue;
using DTO.WebApi;
using DocumentStorageWebApi.Controllers;
using DocumentStorageWebApi.Controllers.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DocumentStorageWebApi.Tests
{
    /// <summary>
    /// Тесты для контроллера DocumentsStorageController.
    /// Покрывают валидацию загружаемых файлов и основные сценарии API.
    /// </summary>
    /// <remarks>
    /// По сути проверяем <see cref="DocumentsStorageController"/> с помощью моков репозитория и очереди.
    /// </remarks>
    public class DocumentsStorageControllerTests
    {
        private readonly IDocumentProcessingQueueService _queueService;
        private readonly IDocumentStorageRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentsStorageController> _logger;
        private readonly DocumentsStorageController _controller;

        public DocumentsStorageControllerTests()
        {
            _queueService = Substitute.For<IDocumentProcessingQueueService>();
            _repository = Substitute.For<IDocumentStorageRepository>();
            _mapper = Substitute.For<IMapper>();
            _logger = Substitute.For<ILogger<DocumentsStorageController>>();

            _controller = new DocumentsStorageController(_queueService, _repository, _mapper, _logger);
        }

        #region Upload — Валидация входных параметров

        [Fact]
        public async Task Upload_FileIsNull_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Upload(null!, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task Upload_FileIsEmpty_ReturnsBadRequest()
        {
            // Arrange
            var emptyFile = Substitute.For<IFormFile>();
            emptyFile.Length.Returns(0);

            // Act
            var result = await _controller.Upload(emptyFile, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task Upload_NonPdfContentType_ReturnsBadRequest()
        {
            // Arrange
            var nonPdfFile = Substitute.For<IFormFile>();
            nonPdfFile.Length.Returns(100);
            nonPdfFile.ContentType.Returns("image/png");
            nonPdfFile.FileName.Returns("test.png");

            // Act
            var result = await _controller.Upload(nonPdfFile, CancellationToken.None);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task Upload_FileSizeExceedsLimit_Returns413()
        {
            // Arrange
            var bigFile = Substitute.For<IFormFile>();
            bigFile.Length.Returns(Common.Constants.MaxDocumentSize + 1);

            // Act
            var result = await _controller.Upload(bigFile, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Upload_ValidPdfFile_ReturnsCreated()
        {
            // Arrange
            var pdfFile = Substitute.For<IFormFile>();
            var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
            pdfFile.Length.Returns(fileContent.Length);
            pdfFile.ContentType.Returns("application/pdf");
            pdfFile.FileName.Returns("document.pdf");

            // Настраиваем фейковый стрим для CopyToAsync
            var memoryStream = new MemoryStream(fileContent);
            pdfFile.OpenReadStream().Returns(memoryStream);
            pdfFile
                .When(x => x.CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()))
                .Do(callInfo =>
                {
                    var targetStream = callInfo.Arg<Stream>();
                    memoryStream.CopyTo(targetStream);
                });

            // Настройка маппера
            var queueDto = new QueueDocumentDto
            {
                Id = Guid.NewGuid(),
                FileName = "document.pdf",
                FileType = DocumentFileType.Pdf,
                FileSize = fileContent.Length,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.InQueue
            };
            _mapper.Map<QueueDocumentDto>(Arg.Any<QueueDocumentDto>()).Returns(queueDto);
            _mapper.Map<NewDocumentResponseDto>(Arg.Any<QueueDocumentDto>()).Returns(new NewDocumentResponseDto());
            _mapper.Map<Domain.Document>(Arg.Any<QueueDocumentDto>()).Returns(new Domain.Document());

            // Act
            var result = await _controller.Upload(pdfFile, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(DocumentsStorageController.Get), createdResult.ActionName);

            // Проверяем, что документ сохранён и сообщение отправлено в очередь
            await _repository.Received(1).AddAsync(Arg.Any<Domain.Document>());
            await _queueService.Received(1).PublishAsync(Arg.Any<QueueDocumentDto>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region Get — Получение документов

        [Fact]
        public async Task Get_ById_ExistingDocument_ReturnsOk()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = new Domain.Document
            {
                Id = documentId,
                FileName = "test.pdf",
                FileType = DocumentFileType.Pdf,
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.Processed
            };
            _repository.GetAsync(documentId).Returns(document);
            _mapper.Map<DocumentListItemDto>(document).Returns(new DocumentListItemDto());

            // Act
            var result = await _controller.Get(documentId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Get_ById_NonExistingDocument_ReturnsNotFound()
        {
            // Arrange
            _repository.GetAsync(Arg.Any<Guid>()).Returns((Domain.Document?)null);

            // Act
            var result = await _controller.Get(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Get_WithQueryParameters_CallsBrowseAsync()
        {
            // Arrange
            var queryParams = new QueryParameters
            {
                Page = 1,
                Size = 20,
                SortBy = "FileName",
                SortOrder = "asc"
            };

            _repository.BrowseAsync(queryParams.Size, queryParams.Page, queryParams.SortBy, queryParams.SortOrder)
                .Returns(Enumerable.Empty<Domain.Document>());

            // Act
            var result = await _controller.Get(queryParams);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            await _repository.Received(1).BrowseAsync(queryParams.Size, queryParams.Page, queryParams.SortBy, queryParams.SortOrder);
        }

        #endregion

        #region GetDocumentText — Получение текста документа

        [Fact]
        public async Task GetDocumentText_ExistingDocument_ReturnsOk()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _repository.GetAsync(documentId).Returns(new Domain.Document
            {
                Id = documentId,
                FileText = "Извлечённый текст документа"
            });

            // Act
            var result = await _controller.GetDocumentText(documentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Извлечённый текст документа", okResult.Value);
        }

        [Fact]
        public async Task GetDocumentText_NonExistingDocument_ReturnsNotFound()
        {
            // Arrange
            _repository.GetAsync(Arg.Any<Guid>()).Returns((Domain.Document?)null);

            // Act
            var result = await _controller.GetDocumentText(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion
    }
}