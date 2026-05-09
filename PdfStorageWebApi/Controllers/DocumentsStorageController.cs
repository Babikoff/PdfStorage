using AutoMapper;
using Common;
using Domain;
using DTO.Queue;
using DTO.WebApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PdfStorageWebApi.Controllers.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WebApiCommon;

namespace PdfStorageWebApi.Controllers
{
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [Route("v{v:apiVersion}/DocumentFiles")]
    [ApiController]
    public class DocumentsStorageController : Controller
    {
        private readonly IDocumentStorageRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentsStorageController> _logger;

        public DocumentsStorageController(IDocumentStorageRepository repository, IMapper mapper, ILogger<DocumentsStorageController> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Get([FromQuery]QueryParameters queryParameters) 
        {
            var pdfFiles = await _repository.BrowseAsync(queryParameters.Size, queryParameters.Page, queryParameters.SortBy, queryParameters.SortOrder);
            return Ok(pdfFiles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var pdfFile = await _repository.GetAsync(id);
            if (pdfFile == null)
            {
                return NotFound();
            }

            return Ok(pdfFile);
        }

        [HttpPost("/UploadLargeDocument")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(Constants.MaxDocumentSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = Constants.MaxDocumentSize)]
        public async Task<ActionResult<NewDocumentDto>> Upload(
            /*[FromForm]*/ IFormFile file,
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

            if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)) {
                return BadRequest(new { message = "Разрешена загрузка только Pdf-файлов." });
            }

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

                await _repository.AddAsync(newDocument);

                var newDocumentDto =_mapper.Map<NewDocumentResponseDto>(newDocument);

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
