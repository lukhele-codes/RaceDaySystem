using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// What the API sends back
public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
}

// What the client sends to create or update an event
public class EventWriteDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = "";
}