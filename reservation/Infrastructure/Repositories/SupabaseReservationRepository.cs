using Application.Interfaces;
using Domain.Entities;
using Supabase;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Responses;

namespace Infrastructure.Repositories;
public class SupabaseReservationRepository : IReservationRepository
{
    private readonly Client _supabase;

    public SupabaseReservationRepository(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<Reservation> CreateAsync(Reservation reservation)
    {
        var result = await _supabase.From<Reservation>().Insert(reservation);
        return result.Models.First();
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        var result = await _supabase.From<Reservation>().Where(x => x.Id == id).Get();
        return result.Models.FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(int id)
    {   
        // Verify record no longer exists
        var check = await _supabase.From<Reservation>()
            .Where(x => x.Id == id)
            .Get();
        
        if(!check.Models.Any())
            return false; //found no database entry with that ID

        await _supabase.From<Reservation>().Where(x => x.Id == id).Delete();

        // Verify record no longer exists
        check = await _supabase.From<Reservation>()
            .Where(x => x.Id == id)
            .Get();
        
        return !check.Models.Any(); // True if successfully deleted
    }

    public async Task<bool> IsSeatAvailableAsync(int playId, int seatNumber)
    {
        var result = await _supabase.From<Reservation>()
            .Where(x => x.PlayId == playId && x.SeatNumber == seatNumber)
            .Get();
        return !result.Models.Any();
    }
}
