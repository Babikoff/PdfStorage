using AutoMapper;
using Domain;
using DTO.Queue;
using RabbitMqService;
using Repository;
using WebApiCommon;

namespace BackgroundFileReciever
{
    public class Worker : BackgroundService
    {
        private readonly IDocumentProcessingQueueService _queueService;
        private readonly IDocumentStorageRepository _documentStorageRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<Worker> _logger;

        public Worker(IDocumentProcessingQueueService rabbitMqService, IDocumentStorageRepository documentStorageRepository, IMapper mapper, ILogger<Worker> logger)
        {
            _queueService = rabbitMqService;
            _documentStorageRepository = documentStorageRepository;
            _mapper = mapper;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker starting. Connecting to queue via RabbitMqService...");

            await _queueService.ConsumeAsync(
                onMessageAsync: async (QueueDocumentDto dto) =>
                {
                    _logger.LogInformation("Processing document: {FileName} (Id: {Id})", dto.FileName, dto.Id);

                    var documentEntity = _mapper.Map<Document>(dto);

                    await _documentStorageRepository.AddAsync(documentEntity);

                    await Task.CompletedTask;
                },
                cancellationToken: stoppingToken);

            _logger.LogInformation("Worker is stopping.");
        }
    }
}