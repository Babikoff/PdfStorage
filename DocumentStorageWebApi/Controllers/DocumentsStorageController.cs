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
using WebApiCommon;

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
        private readonly IPdfTextExtractor _pdfTextExtractor;
        private readonly ILogger<DocumentsStorageController> _logger;

        public DocumentsStorageController(
            IDocumentProcessingQueueService queueService,
            IDocumentStorageRepository repository, 
            IMapper mapper,
            IPdfTextExtractor pdfTextExtractor,
            ILogger<DocumentsStorageController> logger
            )
        {
            _queueService = queueService;
            _repository = repository;
            _mapper = mapper;
            _pdfTextExtractor = pdfTextExtractor;
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

                switch (document.FileType) 
                {
                    case DocumentFileType.Pdf:
                        return Ok(_pdfTextExtractor.ExtractText(document.Data));
                    case DocumentFileType.Unknown:
                        return StatusCode(
                            StatusCodes.Status501NotImplemented, 
                            new { message = "Операция не поддерживается для данного типа документов." }
                            );
                    default:
                        return StatusCode(StatusCodes.Status500InternalServerError);
                }
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

                var newDocument = new Document
                {
                    Id = Guid.NewGuid(),
                    Data = docMemoryStream.ToArray(),
                    FileName = Path.GetFileName(file.FileName),
                    FileType = ParseContentType(file.ContentType),
                    FileSize = file.Length,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _queueService.PublishAsync(_mapper.Map<QueueDocumentDto>(newDocument), cancellationToken);

                //TODO: изменить возвращаемый ответ. Добавить статус в документ InQueue, Processing/Processed/Inserted
                var newDocumentDto =_mapper.Map<NewDocumentResponseDto>(newDocument);
                _logger.LogTrace("File {FileName} with Id={Id} added to the queue.", file.FileName, newDocumentDto.Id);
                return CreatedAtAction(nameof(Get), new { id = newDocument.Id }, newDocumentDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB file saving error {FileName}", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = serverErrorMessageToUser });
            }
            catch (Exception ex) {
                _logger.LogError(ex, "File saving error {FileName}", file.FileName);

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
