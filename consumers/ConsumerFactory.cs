using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace queue_consumer
{
    public class ConsumerFactory : AbstractFactory
    {
        private readonly INotifier _notifier;
        public ConsumerFactory(ILogger logger, IConfiguration configuration, INotifier notifier) : base(logger, configuration)
        {
            _notifier = notifier;
        }
        public IConsumer BuildAzureServiceBusConsumer()
        {
            return new AzureServiceBusConsumer(
                ReadConfigurationValue("TargetServiceName"),
                ReadConfigurationValue("TargetServicePath"),
                ReadConfigurationValue("ServiceBusConnectionString"),
                ReadConfigurationValue("QueueName"),
                _notifier,
                _logger
            );
        }

        public IConsumer BuildRabbitMQConsumer()
        {
            return new RabbitMQConsumer(
                ReadConfigurationValue("TargetServiceName"),
                ReadConfigurationValue("TargetServicePath"),
                ReadConfigurationValue("RabbitMQConnectionString"),
                ReadConfigurationValue("QueueName"),
               _notifier,
               _logger
            );
        }
    }
}