namespace Application.DTOs;

public class SeatEvent
{
    public int SeatNumber { get; set; }
    public int UserId { get; set; }
    public string? Action { get; set; }
    public DateTime Timestamp { get; set; }
}