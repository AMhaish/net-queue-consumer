namespace queue_consumer
{
    public interface INotifier
    {
        void SendNotification(string title, string message);
    }
}