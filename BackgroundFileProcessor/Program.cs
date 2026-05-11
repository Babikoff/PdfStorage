using BackgroundFileProcessor;
using MappingProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PdfService;
using RabbitMQ.Client;
using RabbitMqService;
using Repository;
using Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<DocumentStorageDbContext>((sp, options) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    options.UseNpgsql(configuration.GetConnectionString("PostgresConnection"));
});

// Регистрация сервиса для очереди
builder.Services.AddSingleton<IConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var factory = new ConnectionFactory
    {
        HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
        Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
        UserName = configuration["RabbitMQ:UserName"] ?? "rabbitmq",
        Password = configuration["RabbitMQ:Password"] ?? "rabbitmq"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IDocumentProcessingQueueService>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<RabbitMqService.RabbitMqService>>();
    var queueName = configuration["RabbitMQ:QueueName"] ?? "document-files-queue";
    return new RabbitMqService.RabbitMqService(connection, queueName, logger);
});

// Регистрация сервиса для долгосрочного хранения данных (базы данных)
builder.Services.AddScoped<IDocumentStorageRepository, DocumentStorageRepository>();

// Регистрация сервиса извлечения текста из Pdf
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();

// Регистрация мапперов
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();