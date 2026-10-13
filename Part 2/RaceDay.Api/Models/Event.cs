namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Added for Part 2: the brief requires each event to capture a distance
    // and an event type (Run, Walk or Cycle). Documented in the README as a
    // deliberate change from the Part 1 ERD.
    public string EventType { get; set; } = "Run";
    public decimal DistanceKm { get; set; }

    public int OrganiserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
