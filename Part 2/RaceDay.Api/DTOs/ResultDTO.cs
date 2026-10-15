using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

public class ResultDto
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public DateTime CapturedAt { get; set; }
}

// Used for updates (PUT)
public class ResultWriteDto
{
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }
}

// Used for creating (POST): same fields plus which enrolment it belongs to
public class ResultCreateDto : ResultWriteDto
{
    [Required]
    public int EnrolmentId { get; set; }
}