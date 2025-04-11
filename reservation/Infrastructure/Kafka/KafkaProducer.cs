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
        
        var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<int, int>(producerConfig)
            .SetKeySerializer(Serializers.Int32)
            .SetValueSerializer(Serializers.Int32)
            .Build();
        
        var adminConfig = new AdminClientConfig { BootstrapServers = bootstrapServers };
        _adminClient = new AdminClientBuilder(adminConfig).Build();
    }

    public async Task PublishAsync(string topic, int key, int value)
    {
        await EnsureTopicExists(topic);
        
        try
        {
            await _producer.ProduceAsync(topic, new Message<int, int> 
            { 
                Key = key,
                Value = value 
            });
            _logger.LogInformation("Message successfully published to {Topic} (Key: {Key}, Value: {Value})", 
                topic, key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to topic {Topic}", topic);
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