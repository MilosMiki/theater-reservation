using Domain.Entities;
using Supabase;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class SupabaseReservationRepository : IDisposable
{
    private readonly Client _supabase;
    private readonly ILogger<SupabaseReservationRepository> _logger;
    private readonly IProducer<string, string> _kafkaProducer;
    private readonly IConsumer<string, string> _kafkaConsumer;
    private readonly string _kafkaTopic = "play-events";

    public SupabaseReservationRepository(
        Client supabase,
        ILogger<SupabaseReservationRepository> logger,
        IProducer<string, string> kafkaProducer,
        IConsumer<string, string> kafkaConsumer)
    {
        _supabase = supabase;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
        _kafkaConsumer = kafkaConsumer;
    }

    public async Task<Reservation> CreateReservationAsync(Reservation reservation)
    {
        var kafkaKey = $"{reservation.PlayId}_{reservation.SeatNumber}";
        
        try
        {
            // Check seat availability in Kafka
            var isAvailable = await IsSeatAvailableInKafka(reservation.PlayId, reservation.SeatNumber);
            if (!isAvailable)
            {
                _logger.LogWarning("Seat {SeatNumber} for play {PlayId} is already taken according to Kafka events",
                    reservation.SeatNumber, reservation.PlayId);
                throw new InvalidOperationException("Seat already taken");
            }

            // Push Kafka event first (event sourcing pattern)
            var kafkaMessage = new Message<string, string>
            {
                Key = kafkaKey,
                Value = reservation.UserId.ToString(),
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            await _kafkaProducer.ProduceAsync(_kafkaTopic, kafkaMessage);
            _logger.LogInformation("Successfully pushed Kafka event for reservation {PlayId}_{SeatNumber}",
                reservation.PlayId, reservation.SeatNumber);

            // Then insert into Supabase
            var result = await _supabase.From<Reservation>().Insert(reservation);
            _logger.LogInformation("Successfully created reservation with ID {Id}.", result.Models.First().Id);
            return result.Models.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create reservation for {Key}", kafkaKey);
            throw;
        }
    }

    public async Task<bool> DeleteReservationAsync(int id)
    {
        try
        {
            var reservation = await _supabase.From<Reservation>().Where(x => x.Id == id).Get();
            if (!reservation.Models.Any())
            {
                _logger.LogWarning("Cannot delete reservation with ID {Id} - not found.", id);
                return false;
            }

            var res = reservation.Models.First();
            var kafkaKey = $"{res.PlayId}_{res.SeatNumber}";

            // Verify the user owns this reservation by checking Kafka events
            var latestEvent = await GetLatestKafkaEvent(kafkaKey);
            if (latestEvent?.Value != res.UserId.ToString())
            {
                _logger.LogWarning("User doesn't own reservation {Id} or it's already cancelled", id);
                return false;
            }

            // Push cancellation event
            var kafkaMessage = new Message<string, string>
            {
                Key = kafkaKey,
                Value = "cancelled",
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            await _kafkaProducer.ProduceAsync(_kafkaTopic, kafkaMessage);
            _logger.LogInformation("Pushed cancellation event for {Key}", kafkaKey);

            // Then delete from Supabase
            await _supabase.From<Reservation>().Where(x => x.Id == id).Delete();

            // Verify deletion
            var check = await _supabase.From<Reservation>().Where(x => x.Id == id).Get();
            var success = !check.Models.Any();

            if (success)
                _logger.LogInformation("Successfully deleted reservation with ID {Id}.", id);
            else
                _logger.LogWarning("Failed to delete reservation with ID {Id}.", id);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting reservation with ID {Id}.", id);
            throw;
        }
    }

    public async Task<bool> IsSeatAvailableAsync(int playId, int seatNumber)
    {
        try
        {
            // Check Kafka first (source of truth)
            var kafkaAvailable = await IsSeatAvailableInKafka(playId, seatNumber);
            if (!kafkaAvailable) return false;

            // Then check Supabase as fallback
            var result = await _supabase.From<Reservation>()
                .Where(x => x.PlayId == playId && x.SeatNumber == seatNumber)
                .Get();

            return !result.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check seat availability for play {PlayId}, seat {SeatNumber}.",
                playId, seatNumber);
            throw;
        }
    }

    private async Task<bool> IsSeatAvailableInKafka(int playId, int seatNumber)
    {
        var key = $"{playId}_{seatNumber}";
        var latestEvent = await GetLatestKafkaEvent(key);

        // Seat is available if:
        // 1. No events exist for this key, OR
        // 2. The latest event is a cancellation
        return latestEvent == null || latestEvent.Value == "cancelled";
    }

    private async Task<ConsumeResult<string, string>?> GetLatestKafkaEvent(string key)
    {
        try
        {
            // Create a temporary consumer assigned to the end of the topic
            _kafkaConsumer.Assign(new TopicPartitionOffset(
                new TopicPartition(_kafkaTopic, 0),
                Offset.End));

            // Seek to the beginning to find all messages for this key
            _kafkaConsumer.SeekToBeginning(_kafkaConsumer.Assignment);

            ConsumeResult<string, string>? latestEvent = null;
            var timeout = TimeSpan.FromSeconds(10);
            var startTime = DateTime.UtcNow;

            // Poll for messages until we find the latest one for our key
            while (DateTime.UtcNow - startTime < timeout)
            {
                var consumeResult = _kafkaConsumer.Consume(timeout);
                if (consumeResult == null) break;

                if (consumeResult.Message.Key == key)
                {
                    latestEvent = consumeResult;
                }

                // If we've reached the end of the topic, break
                if (consumeResult.IsPartitionEOF)
                {
                    break;
                }
            }

            return latestEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest Kafka event for key {Key}", key);
            throw;
        }
        finally
        {
            _kafkaConsumer.Unassign();
        }
    }

    public void Dispose()
    {
        _kafkaProducer?.Dispose();
        _kafkaConsumer?.Dispose();
        GC.SuppressFinalize(this);
    }
}