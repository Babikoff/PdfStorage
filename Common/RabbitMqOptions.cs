namespace Common
{
    /// <summary>
    /// Класс для десериализации секции конфигурации RabbitMQ в appsettings.json.
    /// </summary>
    public class RabbitMqOptions
    {
        public const string SectionName = "RabbitMQ";

        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "rabbitmq";
        public string Password { get; set; } = "rabbitmq";
        public string QueueName { get; set; } = "document-files-queue";
    }
}