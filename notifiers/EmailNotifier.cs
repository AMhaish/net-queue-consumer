using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Logging;
using System;

namespace queue_consumer
{
    public class EmailNotifier : INotifier
    {
        private readonly ILogger _logger;
        private readonly string _host;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _from;
        private readonly string _to;
        public EmailNotifier(string host, string userName, string password, string from, string to, ILogger logger)
        {
            _logger = logger;
            _host = host;
            _userName = userName;
            _password = password;
            _from = from;
            _to = to;
        }
        public void SendNotification(string title, string message)
        {
            Task.Run(() =>
            {
                try
                {
                    SmtpClient client = new SmtpClient(_host);
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_userName, _password);
                    client.EnableSsl = true;
                    client.Port = 587;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(_from);
                    var toEmails = _to.Split(',');
                    foreach (var item in toEmails)
                    {
                        mailMessage.To.Add(item);
                    }
                    mailMessage.Body = message;
                    mailMessage.Subject = title;
                    client.Send(mailMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to send the email:" + ex.Message);
                }
            });
        }
    }
}