using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace queue_consumer
{
    public class HostedService : Microsoft.Extensions.Hosting.IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private IConsumer _consumer;
        private INotifier _notifier;
        public HostedService(ILogger<HostedService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running the consumer");
            _notifier = new NotifierFactory(_logger, _configuration).BuildEmailNotifier();
            _notifier.SendNotification("Consumer started", "Consumer started successfully");
            switch (_configuration.GetValue<string>("Consumer"))
            {
                case "AzureSB":
                    _consumer = new ConsumerFactory(_logger, _configuration, _notifier).BuildAzureServiceBusConsumer();
                    break;
                case "RabbitMQ":
                    _consumer = new ConsumerFactory(_logger, _configuration, _notifier).BuildRabbitMQConsumer();
                    break;
                default:
                    _logger.LogError(String.Format("No chosen consumer, the configuration value is:{0}", _configuration.GetValue<string>("Consumer")));
                    break;
            }
            if (_consumer != null) _consumer.Consume();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");
            if (_consumer != null) _consumer.StopConsume();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}