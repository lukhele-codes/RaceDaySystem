using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}

public class CategoryWriteDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; } = 100;

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }
}