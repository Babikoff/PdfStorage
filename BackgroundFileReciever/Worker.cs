using AutoMapper;
using Domain;
using DTO.Queue;
using RabbitMqService;
using Repository;
using System.Reflection.Metadata;
using WebApiCommon;

namespace BackgroundFileReciever
{
    public class Worker : BackgroundService
    {
        private readonly IDocumentProcessingQueueService _queueService;
        private readonly IDocumentStorageRepository _documentStorageRepository;
        private readonly IMapper _mapper;
        private readonly IPdfTextExtractor _pdfTextExtractor;
        private readonly ILogger<Worker> _logger;

        public Worker(
            IDocumentProcessingQueueService rabbitMqService, 
            IDocumentStorageRepository documentStorageRepository, 
            IMapper mapper,
            IPdfTextExtractor pdfTextExtractor,
            ILogger<Worker> logger
            )
        {
            _queueService = rabbitMqService;
            _documentStorageRepository = documentStorageRepository;
            _mapper = mapper;
            _pdfTextExtractor = pdfTextExtractor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker starting. Connecting to queue via RabbitMqService...");

            await _queueService.ConsumeAsync(
                onMessageAsync: async (QueueDocumentDto dto) =>
                {
                    _logger.LogInformation("Processing document: {FileName} (Id: {Id})", dto.FileName, dto.Id);

                    var documentEntity = _mapper.Map<Domain.Document>(dto);

                    switch (documentEntity.FileType)
                    {
                        case DocumentFileType.Pdf:
                            documentEntity.FileText = string.Join('\n', _pdfTextExtractor.ExtractText(dto.RawFileData));
                            break;
                        case DocumentFileType.Unknown:
                            documentEntity.FileText = "";
                            break;
                        default:
                            documentEntity.FileText = "";
                            break;
                    }

                    documentEntity.ProcessingStatus = DocumentProcessingStatus.Processed;
                    documentEntity.ProcessedAt = DateTime.UtcNow;
                    await _documentStorageRepository.AddOrUpdate(documentEntity);

                    await Task.CompletedTask;
                },
                cancellationToken: stoppingToken
            );

            _logger.LogInformation("Worker is stopping.");
        }
    }
}