using System.ComponentModel.DataAnnotations;

namespace RaceDay_WEB.Models
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string? Name { get; set; }
        public double DistanceKm { get; set; }
        public int MaxParticipants { get; set; }
        public double EntryFee { get; set; }
    }

    public class CategoryFormViewModel
    {
        public int Id { get; set; }
        public int EventId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Name can be at most 100 characters.")]
        [Display(Name = "Category name")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Distance is required.")]
        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public double DistanceKm { get; set; }

        [Required(ErrorMessage = "Maximum participants is required.")]
        [Range(1, 100000, ErrorMessage = "Maximum participants must be between 1 and 100000.")]
        [Display(Name = "Maximum participants")]
        public int MaxParticipants { get; set; } = 100;

        [Required(ErrorMessage = "Entry fee is required.")]
        [Range(0, 100000, ErrorMessage = "Entry fee must be between 0 and 100000.")]
        [Display(Name = "Entry fee (R)")]
        public double EntryFee { get; set; }
    }

    // What the API returns when an organiser views an event's enrolments.
    public class EventEnrolmentDto
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int ParticipantId { get; set; }
        public string? ParticipantName { get; set; }
        public string? ParticipantEmail { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string? Status { get; set; }
    }
}