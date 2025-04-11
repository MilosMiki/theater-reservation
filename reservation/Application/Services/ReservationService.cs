using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;
public class ReservationService
{
    private readonly IReservationRepository _repository;
    private readonly KafkaProducer _producer;

    public ReservationService(IReservationRepository repository, KafkaProducer producer)
    {
        _repository = repository;
        _producer = producer;
    }

    public async Task<Reservation> CreateReservationAsync(Reservation reservation)
    {
        if(reservation.PlayId == null)
            throw new Exception("No play selected.");

        if (!await _repository.IsSeatAvailableAsync(reservation.PlayId, reservation.SeatNumber))
            throw new Exception("Seat already taken.");

        var created = await _repository.CreateAsync(reservation);
        await _producer.PublishAsync("reservations.created", created);
        return created;
    }

    public Task<Reservation?> GetReservationAsync(string id) => _repository.GetByIdAsync(id);
    public Task<bool> CancelReservationAsync(string id) => _repository.DeleteAsync(id);
}
