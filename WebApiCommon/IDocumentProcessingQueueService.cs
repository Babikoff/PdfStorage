using DTO.Queue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApiCommon
{
    /// <summary>
    /// Service for interacting with queue server.
    /// Provides methods to publish messages to and consume messages from a queue.
    /// </summary>
    public interface IDocumentProcessingQueueService
    {
        /// <summary>
        /// Publishes a NewDocumentDto message to the configured queue.
        /// </summary>
        Task PublishAsync(QueueDocumentDto message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consumes messages from the configured queue, invoking the provided handler for each message.
        /// </summary>
        /// <param name="onMessageAsync">Async handler invoked for each received message.</param>
        /// <param name="cancellationToken">Cancellation token to stop consuming.</param>
        Task ConsumeAsync(Func<QueueDocumentDto, Task> onMessageAsync, CancellationToken cancellationToken = default);
    }
}