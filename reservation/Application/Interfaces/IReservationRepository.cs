namespace Application.Interfaces;

using Domain.Entities;

public interface IReservationRepository
{
    Task<Reservation> CreateAsync(Reservation reservation);
    Task<Reservation?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<bool> IsSeatAvailableAsync(int playId, int seatNumber);
}
