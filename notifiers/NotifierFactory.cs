using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace queue_consumer
{
    public class NotifierFactory : AbstractFactory
    {

        public NotifierFactory(ILogger logger, IConfiguration configuration) : base(logger, configuration) { }

        public INotifier BuildEmailNotifier()
        {
            return new EmailNotifier(
                ReadConfigurationValue("NotificationHost"),
                ReadConfigurationValue("NotificationUsername"),
                ReadConfigurationValue("NotificationPassword"),
                ReadConfigurationValue("NotificationFrom"),
                ReadConfigurationValue("NotificationTo"),
                _logger
            );
        }
    }
}