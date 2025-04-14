using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Domain.Entities;

namespace API.Controllers
{
    /// <summary>
    /// Controller for managing reservations
    /// </summary>
    [ApiController]
    [Route("reservations")]
    public class ReservationsController : ControllerBase
    {
        private readonly ReservationService _service;

        public ReservationsController(ReservationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Create a new reservation
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /reservations
        ///     {
        ///        "playId": 0,
        ///        "seatNumber": 0,
        ///        "userId": 0
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Reservation created successfully</response>
        /// <response code="500">Seat already taken</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] ReservationRequest request)
        {
            try
            {
                var reservation = new Reservation
                {
                    PlayId = request.PlayId,
                    SeatNumber = request.SeatNumber,
                    UserId = request.UserId
                };

                var created = await _service.CreateReservationAsync(reservation);
                return Ok(created);
            }
            catch (Exception ex) when (ex.Message.Contains("seat", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(500, "Seat already taken.");
            }
        }

        /// <summary>
        /// Get a reservation by ID
        /// </summary>
        /// <response code="200">Returns the requested reservation</response>
        /// <response code="404">Reservation not found</response>
        [HttpGet("{reservationId}")]
        [ProducesResponseType(typeof(Reservation), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int reservationId)
        {
            var reservation = await _service.GetReservationAsync(reservationId);
            return reservation == null ? NotFound() : Ok(reservation);
        }

        /// <summary>
        /// Delete a reservation
        /// </summary>
        /// <response code="204">Reservation deleted successfully</response>
        /// <response code="404">Reservation not found</response>
        [HttpDelete("{reservationId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int reservationId)
        {
            var success = await _service.CancelReservationAsync(reservationId);
            return success ? NoContent() : NotFound();
        }
    }

    /// <summary>
    /// Request model for creating reservations
    /// </summary>
    public class ReservationRequest
    {
        /// <example>1</example>
        public int PlayId { get; set; }
        
        /// <example>1</example>
        public int SeatNumber { get; set; }
        
        /// <example>1</example>
        public int UserId { get; set; }
    }
}