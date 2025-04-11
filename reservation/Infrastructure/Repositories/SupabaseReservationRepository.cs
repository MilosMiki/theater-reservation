using Application.Interfaces;
using Domain.Entities;
using Supabase;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Responses;

namespace Infrastructure.Repositories;
public class SupabaseReservationRepository : IReservationRepository
{
    private readonly Client _supabase;
    private readonly ILogger<SupabaseReservationRepository> _logger;

    public SupabaseReservationRepository(Client supabase, ILogger<SupabaseReservationRepository> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    public async Task<Reservation> CreateAsync(Reservation reservation)
    {
        try
        {
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

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var check = await _supabase.From<Reservation>().Where(x => x.Id == id).Get();
            if (!check.Models.Any())
            {
                _logger.LogWarning("Cannot delete reservation with ID {Id} - not found.", id);
                return false;
            }

            await _supabase.From<Reservation>().Where(x => x.Id == id).Delete();

            // Verify deletion
            check = await _supabase.From<Reservation>().Where(x => x.Id == id).Get();
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