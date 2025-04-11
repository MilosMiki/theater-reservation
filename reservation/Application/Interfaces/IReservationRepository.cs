namespace Application.Interfaces;

using Domain.Entities;

public interface IReservationRepository
{
    Task<Reservation> CreateAsync(Reservation reservation);
    Task<Reservation?> GetByIdAsync(string id);
    Task<bool> DeleteAsync(string id);
    Task<bool> IsSeatAvailableAsync(string playId, int seatNumber);
}
