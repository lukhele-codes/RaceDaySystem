using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class EnrolmentDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EnrolmentWriteDto
{
    [Required]
    public int CategoryId { get; set; }
}