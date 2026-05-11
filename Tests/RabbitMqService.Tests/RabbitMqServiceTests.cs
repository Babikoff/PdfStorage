using System.Text;
using System.Text.Json;
using Contracts;
using DTO.Queue;
using Domain;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace RabbitMqServiceUnitTests
{
    /// <summary>
    /// Тесты для RabbitMqService.
    /// Проверяют публикацию сообщений, подписку и обработку ошибок.
    /// </summary>
    public class RabbitMqServiceTests
    {
        private readonly IConnection _connection;
        private readonly ILogger<RabbitMqService.RabbitMqService> _logger;
        private const string TestQueueName = "test-queue";

        public RabbitMqServiceTests()
        {
            _connection = Substitute.For<IConnection>();
            _logger = Substitute.For<ILogger<RabbitMqService.RabbitMqService>>();
        }

        #region Проверка обработки нулевых параметров
        [Fact]
        public void Constructor_NullConnection_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RabbitMqService.RabbitMqService(null!, TestQueueName, _logger));
        }

        [Fact]
        public void Constructor_NullQueueName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RabbitMqService.RabbitMqService(_connection, null!, _logger));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new RabbitMqService.RabbitMqService(_connection, TestQueueName, null!));
        }

        [Fact]
        public async Task PublishAsync_NullMessage_ThrowsArgumentNullException()
        {
            var service = new RabbitMqService.RabbitMqService(_connection, TestQueueName, _logger);
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.PublishAsync(null!));
        }
        #endregion

        [Fact]
        public async Task PublishAsync_ValidMessage_DeclaresQueueAndPublishes()
        {
            // Arrange
            var channel = Substitute.For<IChannel>();
            _connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
                .Returns(channel);

            var service = new RabbitMqService.RabbitMqService(_connection, TestQueueName, _logger);

            var message = new QueueDocumentDto
            {
                Id = Guid.NewGuid(),
                FileName = "test.pdf",
                FileType = DocumentFileType.Pdf,
                RawFileData = [0x01, 0x02],
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.InQueue
            };

            // Act
            await service.PublishAsync(message);

            // Assert
            await channel.Received(1).QueueDeclareAsync(
                queue: TestQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: Arg.Any<CancellationToken>());

            await channel.Received(1).BasicPublishAsync(
                exchange: string.Empty,
                routingKey: TestQueueName,
                mandatory: false,
                basicProperties: Arg.Is<BasicProperties>(p =>
                    p.ContentType == "application/json" && p.Persistent == true),
                body: Arg.Any<ReadOnlyMemory<byte>>(),
                cancellationToken: Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task PublishAsync_MessageBody_ContainsSerializedDto()
        {
            // Arrange
            var channel = Substitute.For<IChannel>();
            _connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
                .Returns(channel);

            ReadOnlyMemory<byte> capturedBody = default;
            channel.When(x => x.BasicPublishAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<bool>(),
                    Arg.Any<BasicProperties>(),
                    Arg.Any<ReadOnlyMemory<byte>>(),
                    Arg.Any<CancellationToken>()))
                .Do(callInfo =>
                {
                    capturedBody = callInfo.ArgAt<ReadOnlyMemory<byte>>(4);
                });

            var service = new RabbitMqService.RabbitMqService(_connection, TestQueueName, _logger);

            var message = new QueueDocumentDto
            {
                Id = Guid.NewGuid(),
                FileName = "test.pdf",
                FileType = DocumentFileType.Pdf,
                RawFileData = [0x01, 0x02],
                RecievedAt = DateTimeOffset.UtcNow,
                ProcessingStatus = DocumentProcessingStatus.InQueue
            };

            // Act
            await service.PublishAsync(message);

            // Assert
            var json = Encoding.UTF8.GetString(capturedBody.Span);
            var deserialized = JsonSerializer.Deserialize<QueueDocumentDto>(json);
            Assert.NotNull(deserialized);
            Assert.Equal(message.Id, deserialized.Id);
            Assert.Equal(message.FileName, deserialized.FileName);
            Assert.Equal(message.FileType, deserialized.FileType);
        }

        [Fact]
        public async Task ConsumeAsync_NullCallback_ThrowsArgumentNullException()
        {
            var service = new RabbitMqService.RabbitMqService(_connection, TestQueueName, _logger);
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.ConsumeAsync(null!));
        }

        [Fact]
        public async Task ConsumeAsync_CancelledToken_StopsConsuming()
        {
            // Arrange
            var channel = Substitute.For<IChannel>();
            _connection.CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
                .Returns(channel);

            var service = new RabbitMqService.RabbitMqService(_connection, TestQueueName, _logger);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await service.ConsumeAsync(
                onMessageAsync: _ => Task.CompletedTask,
                cancellationToken: cts.Token);

            // Assert
            await channel.Received(1).BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: Arg.Any<CancellationToken>());

            await channel.Received(1).BasicConsumeAsync(
                queue: TestQueueName,
                autoAck: false,
                consumer: Arg.Any<IAsyncBasicConsumer>(),
                cancellationToken: Arg.Any<CancellationToken>());
        }
    }
}