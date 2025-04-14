using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Domain.Entities;

/// <summary>
/// Represents a theater seat reservation
/// </summary>
[Table("ita_reservations")]
public class Reservation : BaseModel
{
    /// <summary>
    /// The unique identifier for the reservation
    /// </summary>
    /// <example>123</example>
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    /// <summary>
    /// The ID of the play being reserved
    /// </summary>
    /// <example>5</example>
    [Column("play_id")]
    public int PlayId { get; set; }

    /// <summary>
    /// The seat number being reserved
    /// </summary>
    /// <example>42</example>
    [Column("seat_number")]
    public int SeatNumber { get; set; }

    /// <summary>
    /// The timestamp when the reservation was made (UTC) - this is added automatically
    /// </summary>
    /// <example>2023-10-15T14:30:00Z</example>
    [Column("reserved_at")]
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The ID of the user making the reservation
    /// </summary>
    /// <example>789</example>
    [Column("user_id")]
    public int UserId { get; set; }
}
