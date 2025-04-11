using Application.Interfaces;
using Domain.Entities;
using Confluent.Kafka;
using System;

namespace Application.Services;

public class ReservationService
{
    private readonly IReservationRepository _repository;
    private readonly KafkaProducer _producer;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        IReservationRepository repository, 
        KafkaProducer producer,
        ILogger<ReservationService> logger)
    {
        _repository = repository;
        _producer = producer;
        _logger = logger;
    }

    public async Task<Reservation> CreateReservationAsync(Reservation reservation)
    {
        if(reservation.PlayId == -1)
            throw new Exception("No play selected.");

        try
        {
            var topicName = $"play-{reservation.PlayId}";
            
            // Publish to Kafka
            await _producer.PublishAsync(
                topic: topicName,
                key: reservation.SeatNumber,
                value: reservation.UserId
            );
            
            _logger.LogInformation(
                "Published reservation event to {Topic} (Seat: {Seat}, User: {User})",
                topicName, reservation.SeatNumber, reservation.UserId
            );

            // Consumer setup
            using var consumer = new ConsumerBuilder<int, int>(new ConsumerConfig
            {
                BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_URL") ?? "localhost:9093",
                GroupId = $"reservation-verifier-{Guid.NewGuid()}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            })
            .SetKeyDeserializer(Deserializers.Int32)
            .SetValueDeserializer(Deserializers.Int32)
            .Build();

            consumer.Subscribe(topicName);
            
            _logger.LogInformation("Starting verification consumer for topic {Topic}", topicName);

            bool verificationPassed = false;
            DateTime startTime = DateTime.UtcNow;
            TimeSpan timeout = TimeSpan.FromSeconds(5);

            int isSeatTaken = -1;
            while (DateTime.UtcNow - startTime < timeout)
            {
                _logger.LogDebug("Attempting to consume message...");
                
                try
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (consumeResult != null)
                    {
                        _logger.LogInformation(
                            "Consumed message - Seat: {Seat}, User: {User}, Partition: {Partition}, Offset: {Offset}. IsSeatTaken: {temp}",
                            consumeResult.Message.Key,
                            consumeResult.Message.Value,
                            consumeResult.Partition,
                            consumeResult.Offset,
                            isSeatTaken
                        );

                        if (consumeResult.Message.Key == reservation.SeatNumber)
                        {
                            if (consumeResult.Message.Value == -1)
                            {
                                _logger.LogInformation("Found cancellation marker for seat {Seat}", reservation.SeatNumber);
                                isSeatTaken = -1;
                            }
                            else if(isSeatTaken != -1)
                            {
                                continue;
                            }
                            else if (consumeResult.Message.Value == reservation.UserId)
                            {
                                _logger.LogInformation("Successfully verified reservation for seat {Seat}", reservation.SeatNumber);
                                verificationPassed = true;
                                isSeatTaken = reservation.UserId;
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Seat {Seat} already taken by user {ExistingUser} (attempted by {NewUser})",
                                    reservation.SeatNumber,
                                    consumeResult.Message.Value,
                                    reservation.UserId
                                );
                                isSeatTaken = consumeResult.Message.Value;
                            }
                        }
                    }
                }
                catch (ConsumeException e)
                {
                    _logger.LogWarning("Consume error: {Error}", e.Error.Reason);
                }
            }

            if(reservation.UserId != isSeatTaken && isSeatTaken != -1){
                throw new Exception("Seat already taken.");
            }

            if (!verificationPassed)
            {
                _logger.LogWarning("Verification timeout - Failed to confirm reservation for seat {Seat}", reservation.SeatNumber);
                throw new Exception("Failed to verify reservation. Please try again.");
            }

            // Database operation
            var created = await _repository.CreateAsync(reservation);
            _logger.LogInformation("Successfully created reservation {Id} in database", created.Id);
            
            return created;
        }
        catch (Exception ex) when (ex.Message != "Seat already taken.")
        {
            _logger.LogError(ex, "Reservation failed for seat {Seat}", reservation.SeatNumber);
            throw new Exception("Failed to process reservation. Please try again.");
        }
    }

    public Task<Reservation?> GetReservationAsync(int id) => _repository.GetByIdAsync(id);
    
    public async Task<bool> CancelReservationAsync(int id)
    {   
        try
        {
            // 1. Get existing reservation
            var reservation = await _repository.GetByIdAsync(id);
            if (reservation == null)
            {
                _logger.LogWarning("Reservation {Id} not found", id);
                return false;
            }

            // 2. Publish cancellation event to Kafka
            var topicName = $"play-{reservation.PlayId}";
            await _producer.PublishAsync(
                topic: topicName,
                key: reservation.SeatNumber,
                value: -1 // Using -1 to indicate cancellation (since value must be int)
            );
            
            _logger.LogInformation(
                "Published CANCELLATION event to {Topic} (Seat: {Seat})",
                topicName, reservation.SeatNumber
            );

            // 3. Delete from database
            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _logger.LogInformation("Successfully cancelled reservation {Id}", id);
            }
            else
            {
                _logger.LogWarning("Failed to delete reservation {Id} from database", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation {Id}", id);
            throw new Exception("Failed to cancel reservation. Please try again.");
        }
    }
}