using System.ComponentModel.DataAnnotations;

namespace RaceDay_WEB.Models
{
    // What GET api/Enrolments/mine returns.
    public class EnrolmentDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int EventId { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string? Status { get; set; }
    }

    public class MyEnrolmentRow
    {
        public EnrolmentDto Enrolment { get; set; } = new();
        public EventDto? Event { get; set; }
    }

    public class MyResultRow
    {
        public ResultDto Result { get; set; } = new();
        public EventDto? Event { get; set; }
    }

    public class EventDetailViewModel
    {
        public EventDto Event { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public EnrolmentDto? MyEnrolment { get; set; }
    }

    public class ParticipantHomeViewModel
    {
        public List<EventDto> Upcoming { get; set; } = new();
        public int MyEnrolments { get; set; }
        public int Confirmed { get; set; }
        public int ResultsCount { get; set; }
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Name can be at most 100 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        public string? Role { get; set; }
    }

    public static class UiHelpers
    {
        public static string StatusClass(string? status)
        {
            var s = (status ?? "").ToLower();
            if (s.Contains("confirm")) return "status-confirmed";
            if (s.Contains("pend")) return "status-pending";
            if (s.Contains("cancel")) return "status-cancelled";
            return "bg-secondary";
        }

        public static string TypeIcon(string? type) => (type ?? "").ToLower() switch
        {
            "walk" => "bi-person-walking",
            "cycle" => "bi-bicycle",
            _ => "bi-lightning-charge-fill"
        };

        public static string FormatTime(string? finishTime) =>
            TimeSpan.TryParse(finishTime, out var ts) ? ts.ToString(@"hh\:mm\:ss") : "-";
    }
}