using System.Text;
using System.Text.Json;
using DTO.Queue;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Contracts;

namespace RabbitMqService
{
    /// <summary>
    /// Реализация интерфейса работы с очередью <see cref="IDocumentProcessingQueueService"/> на основе RabbitMQ.
    /// </summary>
    public class RabbitMqService : IDocumentProcessingQueueService
    {
        private readonly IConnection _connection;
        private readonly string _queueName;
        private readonly ILogger<RabbitMqService> _logger;

        private const int checkQueueInterval = 20000;

        public RabbitMqService(IConnection connection, string queueName, ILogger<RabbitMqService> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task PublishAsync(QueueDocumentDto message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queueName,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Published message to queue '{QueueName}': {FileName}", _queueName, message.FileName);
        }

        /// <inheritdoc />
        public async Task ConsumeAsync(Func<QueueDocumentDto, Task> onMessageAsync, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(onMessageAsync);

            await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Awaiting messages from queue '{QueueName}'. Polling every {checkQueueInterval} msec.", _queueName, checkQueueInterval);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await channel.BasicGetAsync(_queueName, autoAck: false, cancellationToken: cancellationToken);

                    if (result != null)
                    {
                        var body = result.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);


                        var dto = JsonSerializer.Deserialize<QueueDocumentDto>(body);
                        if (dto != null)
                        {
                            _logger.LogInformation("Received message from queue '{QueueName}'. Message Id: {Id}", _queueName, dto.Id);
                            await onMessageAsync(dto);
                            // Если при обоработке события не было ошибок, то удаляем message из очереди
                            await channel.BasicAckAsync(result.DeliveryTag, false, cancellationToken);
                        }
                        else 
                        {
                            _logger.LogWarning("Dto is null. Queue '{QueueName}'", _queueName);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("No messages in queue '{QueueName}'", _queueName);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while reading from queue '{QueueName}'", _queueName);
                }

                await Task.Delay(checkQueueInterval, cancellationToken);
            }

            _logger.LogInformation("Stopped consuming from queue '{QueueName}'", _queueName);
        }
    }
}