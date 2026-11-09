using System.ComponentModel.DataAnnotations;

namespace RaceDay_WEB.Models
{
    public class ResultDto
    {
        public int Id { get; set; }
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? FinishTime { get; set; }
        public int? Position { get; set; }
        public DateTime CapturedAt { get; set; }
    }

    // One row on the results page: an enrolment plus its result (if captured yet).
    public class ResultRowViewModel
    {
        public EventEnrolmentDto Enrolment { get; set; } = new();
        public ResultDto? Result { get; set; }
    }

    public class ResultFormViewModel
    {
        public int EventId { get; set; }
        public int CategoryId { get; set; }
        public int EnrolmentId { get; set; }
        public int? ResultId { get; set; }
        public string? ParticipantName { get; set; }
        public string? CategoryName { get; set; }

        [RegularExpression(@"^([0-1]?\d|2[0-3]):[0-5]\d:[0-5]\d$",
            ErrorMessage = "Enter the time as hh:mm:ss, for example 00:45:30.")]
        [Display(Name = "Finish time (hh:mm:ss)")]
        public string? FinishTime { get; set; }

        [Range(1, 100000, ErrorMessage = "Position must be between 1 and 100000.")]
        public int? Position { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int TotalEnrolments { get; set; }
        public int ConfirmedEnrolments { get; set; }
        public int PendingEnrolments { get; set; }
        public List<EventDto> NextEvents { get; set; } = new();
    }
}