using AutoMapper;
using Common;
using DocumentStorageWebApi.Controllers.Helpers;
using Domain;
using DTO.Queue;
using DTO.WebApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RabbitMqService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Contracts;

namespace DocumentStorageWebApi.Controllers
{
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [Route("v{v:apiVersion}/DocumentFiles")]
    [ApiController]
    public class DocumentsStorageController : Controller
    {
        private readonly IDocumentProcessingQueueService _queueService;
        private readonly IDocumentStorageRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentsStorageController> _logger;

        public DocumentsStorageController(
            IDocumentProcessingQueueService queueService,
            IDocumentStorageRepository repository, 
            IMapper mapper,
            ILogger<DocumentsStorageController> logger
            )
        {
            _queueService = queueService;
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Get([FromQuery]QueryParameters queryParameters) 
        {
            try
            {
                var documents = 
                    _mapper.Map<IEnumerable<DocumentListItemDto>>(
                        await _repository.BrowseAsync(queryParameters.Size, queryParameters.Page, queryParameters.SortBy, queryParameters.SortOrder)
                        );

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while browsing documents");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ошибка сервера при получении списка документов." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var document = await _repository.GetAsync(id);
                if (document == null)
                {
                    return NotFound();
                }

                return Ok(_mapper.Map<DocumentListItemDto>(document));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting document by id {DocumentId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ошибка сервера при получении документа." });
            }
        }

        [HttpGet("DocumentText/{id}")]
        public async Task<IActionResult> GetDocumentText(Guid id)
        {
            try
            {
                var document = await _repository.GetAsync(id);
                if (document == null)
                {
                    return NotFound();
                }

                return Ok(document.FileText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while extracting text from document {DocumentId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ошибка сервера при извлечении текста документа." });
            }
        }

        [HttpPost("UploadDocument")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(Constants.MaxDocumentSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = Constants.MaxDocumentSize)]
        public async Task<ActionResult<QueueDocumentDto>> Upload(
            IFormFile file,
            CancellationToken cancellationToken
            )
        {
            if (file is null || file.Length == 0) {
                return BadRequest(new { message = "Файл не передан или не содержит данных." });
            }

            if (file.Length > Constants.MaxDocumentSize) {
                return StatusCode(StatusCodes.Status413PayloadTooLarge,
                    new { message = $"Размер файла не должен превышать {Constants.MaxDocumentSize} байт." });
            }

            //TODO: Sanitize/validate file name and ContentType 
            if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) {
                return BadRequest(new { message = "Разрешена загрузка только Pdf-файлов." });
            }

            _logger.LogTrace("Processing {FileName}.", file.FileName);

            const string serverErrorMessageToUser = "Ошибка сервера при загрузке файла";
            try
            {
                using var docMemoryStream = new MemoryStream();
                await file.CopyToAsync(docMemoryStream, cancellationToken);

                var queueDocument = new QueueDocumentDto
                {
                    Id = Guid.NewGuid(),
                    RawFileData = docMemoryStream.ToArray(),
                    FileName = Path.GetFileName(file.FileName),
                    FileType = ParseContentType(file.ContentType),
                    FileSize = file.Length,
                    RecievedAt = DateTimeOffset.UtcNow,
                    ProcessingStatus = DocumentProcessingStatus.InQueue,
                };

                var documentEntity = _mapper.Map<Document>(queueDocument);
                try
                {
                    await _repository.AddAsync(documentEntity);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "DB file saving error {FileName}", file.FileName);
                    // Не смотря на то что при сохранении документа в БД произошла ошибка, продолжим выполнение,
                    // чтобы сохранить документ в очередь и попытаться снова вставить при извлечении из очереди.
                    // Это позволит сохранить полученные данные в случае недоступности БД, но усложняет бизнес-логику.
                    // Возможно, что это не лучшее место для встраивания частичной отказоустойчивости.
                }

                await _queueService.PublishAsync(_mapper.Map<QueueDocumentDto>(queueDocument), cancellationToken);

                var newDocumentDto =_mapper.Map<NewDocumentResponseDto>(queueDocument);
                _logger.LogTrace("File {FileName} with Id={Id} added to the queue.", file.FileName, newDocumentDto.Id);
                return CreatedAtAction(nameof(Get), new { id = queueDocument.Id }, newDocumentDto);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "File handling error {FileName}", file.FileName);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = serverErrorMessageToUser });
            }
        }
        
        private DocumentFileType ParseContentType(string contentType)
        {
            switch (contentType) {
                case "application/pdf":
                    return DocumentFileType.Pdf;
                default:
                    return DocumentFileType.Unknown;
            }
        }
    }

}
