using BackgroundFileProcessor;
using Common;
using MappingProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

// Bind RabbitMQ config section to strongly-typed options
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

// Регистрация сервиса для очереди
builder.Services.AddSingleton<IConnection>(sp =>
{
    var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
    var factory = new ConnectionFactory
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password
    };
    return factory.CreateConnectionAsync().ConfigureAwait(false).GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IDocumentProcessingQueueService>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
    var logger = sp.GetRequiredService<ILogger<RabbitMqService.RabbitMqService>>();
    return new RabbitMqService.RabbitMqService(connection, options.QueueName, logger);
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