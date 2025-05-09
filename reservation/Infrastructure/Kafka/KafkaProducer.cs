using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System.Text.Json;

public class KafkaProducer
{
    private readonly IProducer<int, int> _producer;
    private readonly IAdminClient _adminClient;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(string bootstrapServers, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        
        var producerConfig = new ProducerConfig { 
            BootstrapServers = bootstrapServers, 
            Acks = Acks.All
        };
        _producer = new ProducerBuilder<int, int>(producerConfig)
            .SetKeySerializer(Serializers.Int32)
            .SetValueSerializer(Serializers.Int32)
            .Build();
        
        var adminConfig = new AdminClientConfig { BootstrapServers = bootstrapServers };
        _adminClient = new AdminClientBuilder(adminConfig).Build();
    }

    public async Task PublishAsync(string topic, int key, int value)
    {
        _logger.LogInformation("Entering Publish to Kafka for topic: {topic}", topic);
        await EnsureTopicExists(topic);
        _logger.LogInformation("Checked topic exists: {topic}", topic);
        
        try
        {
            var deliveryReport = await _producer.ProduceAsync(topic, new Message<int, int> 
            { 
                Key = key,
                Value = value 
            });
            _logger.LogInformation(
            "Delivered to: {Topic} [Partition {Partition} @ {Offset}] (Key: {Key})",
            deliveryReport.Topic,
            deliveryReport.Partition,
            deliveryReport.Offset,
            key);
        }
        catch (ProduceException<int, int> ex)
        {
            _logger.LogError(ex, 
                "Failed to deliver to {Topic} (Key: {Key}): {Reason}",
                topic, key, ex.Error.Reason);
            throw;
        }
    }

    private async Task EnsureTopicExists(string topic)
    {
        try
        {
            var metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            if (metadata.Topics.All(t => t.Topic != topic))
            {
                _logger.LogInformation("Creating Kafka topic: {Topic}", topic);
                await _adminClient.CreateTopicsAsync(new[]
                {
                    new TopicSpecification
                    {
                        Name = topic,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    }
                });
            }
        }
        catch (CreateTopicsException e)
        {
            _logger.LogError(e, "Topic creation error for {Topic}: {Reason}", 
                topic, e.Results[0].Error.Reason);
            throw;
        }
    }
}