using Application.Interfaces;
using Domain.Entities;
using Supabase;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Responses;
using System.Text.Json;

namespace Infrastructure.Repositories;
public class SupabaseReservationRepository : IReservationRepository
{
    private readonly Client _supabase;
    private readonly ILogger<SupabaseReservationRepository> _logger;
    private readonly KafkaProducer _kafkaProducer;
    private readonly SeatAvailabilityService _seatAvailabilityService;

    public SupabaseReservationRepository(
        Client supabase, 
        ILogger<SupabaseReservationRepository> logger,
        KafkaProducer kafkaProducer,
        SeatAvailabilityService seatAvailabilityService)
    {
        _supabase = supabase;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
        _seatAvailabilityService = seatAvailabilityService;
    }

    public async Task<Reservation> CreateAsync(Reservation reservation)
    {
        try
        {
            
            if (!_seatAvailabilityService.IsSeatAvailable(reservation.PlayId, reservation.SeatNumber))
            {
                _logger.LogWarning("Seat {SeatNumber} for play {PlayId} is already taken", 
                    reservation.SeatNumber, reservation.PlayId);
                throw new InvalidOperationException("Seat already taken");
            }

            await _kafkaProducer.PublishSeatEventAsync(
                reservation.PlayId,
                reservation.SeatNumber,
                new SeatEvent
                {
                    UserId = reservation.UserId,
                    Action = "reserved",
                    Timestamp = DateTime.UtcNow
            });

            // Create reservation in Supabase
            var result = await _supabase.From<Reservation>().Insert(reservation);
            _logger.LogInformation("Successfully created reservation with ID {Id}.", result.Models.First().Id);
            
            return result.Models.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create reservation.");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var reservation = await GetByIdAsync(id);
            if (reservation == null) return false;
            
            if (!_seatAvailabilityService.IsReservedByUser(
                reservation.PlayId, reservation.SeatNumber, reservation.UserId))
            {
                _logger.LogWarning("User {UserId} doesn't own reservation {Id}", 
                    reservation.UserId, id);
                return false;
            }

            await _kafkaProducer.PublishSeatEventAsync(
                reservation.PlayId,
                reservation.SeatNumber,
                new SeatEvent
                {
                    UserId = reservation.UserId,
                    Action = "cancelled",
                    Timestamp = DateTime.UtcNow
                });

            // Delete from Supabase
            await _supabase.From<Reservation>().Where(x => x.Id == id).Delete();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting reservation with ID {Id}.", id);
            throw;
        }
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        try
        {
            var result = await _supabase.From<Reservation>().Where(x => x.Id == id).Get();
            var res = result.Models.FirstOrDefault();

            if (res == null)
                _logger.LogWarning("Reservation with ID {Id} not found.", id);
            else
                _logger.LogInformation("Successfully fetched reservation with ID {Id}.", id);

            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch reservation with ID {Id}.", id);
            throw;
        }
    }

    public async Task<bool> IsSeatAvailableAsync(int playId, int seatNumber)
    {
        try
        {
            var result = await _supabase.From<Reservation>()
                .Where(x => x.PlayId == playId && x.SeatNumber == seatNumber)
                .Get();

            var available = !result.Models.Any();

            _logger.LogInformation("Seat {SeatNumber} for play {PlayId} is {Status}.", 
                seatNumber, playId, available ? "available" : "taken");

            return available;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check seat availability for play {PlayId}, seat {SeatNumber}.", playId, seatNumber);
            throw;
        }
    }
}

// Event model for Kafka messages
public class SeatEvent
{
    public int SeatNumber { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } // "reserved" or "cancelled"
    public DateTime Timestamp { get; set; }
}