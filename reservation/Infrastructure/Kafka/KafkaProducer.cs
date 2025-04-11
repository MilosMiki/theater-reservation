using Confluent.Kafka;
using System.Text.Json;

public class KafkaProducer
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(string bootstrapServers, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublishAsync(string topic, object message)
    {
        var json = JsonSerializer.Serialize(message);
        try
        {
            await _producer.ProduceAsync(topic, new Message<Null, string> { Value = json });
            _logger.LogInformation("Message successfully published to {TopicPartitionOffset}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to topic {Topic}", topic);
            throw;
        }
    }
}
