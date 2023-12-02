using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace queue_consumer
{
    public class RabbitMQConsumer : IConsumer
    {
        private string _connectionString;
        private string _queueName;
        private string _targetServiceName;
        private string _targetServiceAddress;
        private INotifier _notifier;
        private readonly ILogger _logger;
        private static IModel channel;
        private static IConnection connection;
        private static EventingBasicConsumer consumer;
        private static string consumerTag;
        public RabbitMQConsumer(string targetServiceName, string targetServiceAddress, string connectionString, string queueName, INotifier notifier, ILogger logger)
        {
            _targetServiceName = targetServiceName;
            _targetServiceAddress = targetServiceAddress;
            _connectionString = connectionString;
            _queueName = queueName;
            _notifier = notifier;
            _logger = logger;
        }

        public void Consume()
        {
            var factory = new ConnectionFactory();
            factory.Uri = new System.Uri(_connectionString);
            factory.RequestedHeartbeat = TimeSpan.FromSeconds(60);
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            Dictionary<string, object> args = new Dictionary<string, object>()
            {
                { "x-queue-type", "quorum" }
            }; 
            channel.QueueDeclare(_queueName, true,  false,  false, args);
            consumer = new EventingBasicConsumer(channel);
            consumer.Received += ProcessMessagesAsync;
            consumerTag = channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation($"Received consumer TAG: {consumerTag}");
        }

        public void StopConsume()
        {
            _logger.LogInformation($"Stopping consuming");
            channel.BasicCancel(consumerTag);
        }

        void ProcessMessagesAsync(object sender, BasicDeliverEventArgs ea)
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            string traceId = null;
            string spanId = null;
            string uniqueId = null;
            var body = (JObject)JsonConvert.DeserializeObject(message);
            if (body.ContainsKey("traceId") && body.ContainsKey("spanId"))
            {
                traceId = body["traceId"].Value<String>();
                spanId = body["spanId"].Value<String>();
                if(body.ContainsKey("outerConversationID"))
                    uniqueId = body["outerConversationID"].Value<String>();
            }
            // Process the message.
            _logger.LogInformation($"Received message: Body:{message}" + (traceId != null ? "%%" + traceId : "") + (spanId != null ? "%%" + spanId : "") + (uniqueId != null ? "%%" + uniqueId : ""));
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(message, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("X-B3-TraceId", traceId);
                client.DefaultRequestHeaders.Add("X-B3-SpanId", spanId);
                try
                {
                    _logger.LogInformation($"Posting the message" + (traceId != null ? "%%" + traceId : "") + (spanId != null ? "%%" + spanId : "") + (uniqueId != null ? "%%" + uniqueId : ""));
                    // No need for retry logic because in case of exception the message will be re-queued through BasicNack
                    client.PostAsync(_targetServiceAddress, content).ContinueWith(async (responseTask) =>
                    {
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        var response = responseTask.Result;
                        _logger.LogInformation($"Response Received" + (traceId != null ? "%%" + traceId : "") + (spanId != null ? "%%" + spanId : "") + (uniqueId != null ? "%%" + uniqueId : ""));
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var conversationId = "-";
                        if (body.ContainsKey("conversationID"))
                        {
                            conversationId = body["conversationID"].Value<String>();
                        }
                        _logger.LogInformation($"{response.StatusCode} status code from {_targetServiceName} for {conversationId}:" + (responseBody ?? " No info") + (traceId != null ? "%%" + traceId : "") + (spanId != null ? "%%" + spanId : "") + (uniqueId != null ? "%%" + uniqueId : ""));
                    }).Wait();
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError($"\nException Caught for {body["traceId"].Value<String>()}!" + e.Message + (traceId != null ? "%%" + traceId : "") + (spanId != null ? "%%" + spanId : "") + (uniqueId != null ? "%%" + uniqueId : ""));
                    _notifier.SendNotification($"\nException Caught for {body["traceId"].Value<String>()}!", "Message:" + e.Message);
                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"\nException Caught for {body["traceId"].Value<String>()}!" + ex.Message + (traceId != null ? "%%" + traceId : "") + (spanId != null ? "%%" + spanId : "") + (uniqueId != null ? "%%" + uniqueId : ""));
                    _notifier.SendNotification($"\nException Caught for {body["traceId"].Value<String>()}!", "Message:" + ex.Message);
                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
            }
        }
    }
}