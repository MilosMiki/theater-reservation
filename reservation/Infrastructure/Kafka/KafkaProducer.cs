using Application.DTOs;
using Confluent.Kafka;
using Confluent.Kafka.Admin; // Add this for topic management
using System.Text.Json;

public class KafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly string _bootstrapServers;

    public KafkaProducer(IConfiguration config, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        _bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_URL") ?? "localhost:9093";
        
        var producerConfig = new ProducerConfig { BootstrapServers = _bootstrapServers };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishSeatEventAsync(int playId, int seatNumber, SeatEvent message)
    {
        var topicName = $"play_{playId}";
        
        try
        {
            var json = JsonSerializer.Serialize(message);
            await _producer.ProduceAsync(
                topicName, 
                new Message<string, string>
                {
                    Key = seatNumber.ToString(),
                    Value = json
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing seat event");
            throw;
        }
    }
}