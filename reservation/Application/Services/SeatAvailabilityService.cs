using Application.DTOs;
using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Text.Json; // Add this
using System.Text.RegularExpressions; // Add this for Regex

public class SeatAvailabilityService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<SeatAvailabilityService> _logger;
    private readonly ConcurrentDictionary<string, SeatState> _seatStates = new();
    private readonly string _bootstrapServers;

    private class SeatState
    {
        public int UserId { get; set; }
        public string Status { get; set; } = "available";
        public DateTime LastUpdated { get; set; }
    }

    public SeatAvailabilityService(IConfiguration config, ILogger<SeatAvailabilityService> logger)
    {
        _logger = logger;
        _bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_URL") ?? "localhost:9093";
        
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "seat-availability-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(new Regex("play_\\d+"));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    var message = JsonSerializer.Deserialize<SeatEvent>(consumeResult.Message.Value);
                    
                    if (message != null)
                    {
                        var playId = consumeResult.Topic.Replace("play_", "");
                        var seatKey = $"{playId}_{consumeResult.Message.Key}";

                        _seatStates.AddOrUpdate(seatKey,
                            key => new SeatState
                            {
                                UserId = message.UserId,
                                Status = message.Action,
                                LastUpdated = message.Timestamp
                            },
                            (key, existing) =>
                            {
                                if (message.Timestamp > existing.LastUpdated)
                                {
                                    existing.UserId = message.UserId;
                                    existing.Status = message.Action;
                                    existing.LastUpdated = message.Timestamp;
                                }
                                return existing;
                            });
                    }
                }
                catch (ConsumeException e)
                {
                    _logger.LogWarning(e, "Temporary Kafka error");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    public bool IsSeatAvailable(int playId, int seatNumber)
    {
        var seatKey = $"{playId}_{seatNumber}";
        return !_seatStates.TryGetValue(seatKey, out var state) || 
               state.Status == "available" || 
               state.Status == "cancelled";
    }

    public bool IsReservedByUser(int playId, int seatNumber, int userId)
    {
        var seatKey = $"{playId}_{seatNumber}";
        return _seatStates.TryGetValue(seatKey, out var state) && 
               state.Status == "reserved" && 
               state.UserId == userId;
    }
}