using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Entities;

[ApiController]
[Route("reservations")]
public class ReservationsController : ControllerBase
{
    private readonly ReservationService _service;

    public ReservationsController(ReservationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Reservation reservation)
    {
        var created = await _service.CreateReservationAsync(reservation);
        return CreatedAtAction(nameof(Get), new { reservationId = created.Id }, created);
    }

    [HttpGet("{reservationId}")]
    public async Task<IActionResult> Get(string reservationId)
    {
        var reservation = await _service.GetReservationAsync(reservationId);
        return reservation == null ? NotFound() : Ok(reservation);
    }

    [HttpDelete("{reservationId}")]
    public async Task<IActionResult> Delete(string reservationId)
    {
        var success = await _service.CancelReservationAsync(reservationId);
        return success ? NoContent() : NotFound();
    }
}
