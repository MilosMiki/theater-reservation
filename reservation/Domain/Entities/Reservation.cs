using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Domain.Entities;

[Table("reservations")]
public class Reservation : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("play_id")]
    public string? PlayId { get; set; }

    [Column("seat_number")]
    public int SeatNumber { get; set; }

    [Column("reserved_at")]
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    [Column("user_email")]
    public string? UserEmail { get; set; }
}
