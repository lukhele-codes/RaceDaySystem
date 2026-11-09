using System.ComponentModel.DataAnnotations;

namespace RaceDay_WEB.Models
{
    // Matches what the API returns.
    public class EventDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string? Location { get; set; }
        public string? EventType { get; set; }
        public double DistanceKm { get; set; }
        public int OrganiserId { get; set; }
    }

    // Used by the create and edit form.
    public class EventFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(150, ErrorMessage = "Name can be at most 150 characters.")]
        [Display(Name = "Event name")]
        public string Name { get; set; } = "";

        [StringLength(1000, ErrorMessage = "Description can be at most 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please choose a date and time.")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        [Display(Name = "Date and time")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(200, ErrorMessage = "Location can be at most 200 characters.")]
        public string Location { get; set; } = "";

        [Required(ErrorMessage = "Please choose an event type.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "Run";

        [Required(ErrorMessage = "Distance is required.")]
        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public double DistanceKm { get; set; }
    }
}