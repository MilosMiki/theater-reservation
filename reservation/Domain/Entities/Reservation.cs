using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Domain.Entities;

[Table("ita_reservations")]
public class Reservation : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("play_id")]
    public int PlayId { get; set; }

    [Column("seat_number")]
    public int SeatNumber { get; set; }

    [Column("reserved_at")]
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    [Column("user_id")]
    public int UserId { get; set; }
}
