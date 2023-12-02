namespace queue_consumer
{
    public interface IConsumer
    {
        void Consume();

        void StopConsume();
    }
}