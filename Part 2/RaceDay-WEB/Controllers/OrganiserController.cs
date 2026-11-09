using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay_WEB.Models;
using RaceDay_WEB.Services;
using System.Net;

namespace RaceDay_WEB.Controllers
{
    [SessionAuthorize("Organiser")]
    public class OrganiserController : Controller
    {
        private readonly ApiService _api;

        public OrganiserController(ApiService api)
        {
            _api = api;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        // ---------- Events ----------

        public async Task<IActionResult> Events(string? search)
        {
            var url = "api/Events";
            if (!string.IsNullOrWhiteSpace(search))
                url += "?search=" + Uri.EscapeDataString(search);

            var result = await _api.GetAsync<List<EventDto>>(url);
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success) TempData["FlashError"] = result.Error;

            // An organiser only manages their own events.
            var userId = HttpContext.Session.GetInt32("UserId");
            var events = (result.Data ?? new List<EventDto>())
                .Where(e => e.OrganiserId == userId)
                .OrderBy(e => e.EventDate)
                .ToList();

            ViewBag.Search = search;
            return View(events);
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            ViewData["Heading"] = "Create event";
            return View("EventForm", new EventFormViewModel
            {
                EventDate = DateTime.Today.AddDays(14).AddHours(7)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormViewModel model)
        {
            ViewData["Heading"] = "Create event";
            if (!ModelState.IsValid) return View("EventForm", model);

            var result = await _api.SendAsync(HttpMethod.Post, "api/Events", ToPayload(model));
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                return View("EventForm", model);
            }

            TempData["Flash"] = "Event created.";
            return RedirectToAction("Events");
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var result = await _api.GetAsync<EventDto>($"api/Events/{id}");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success || result.Data == null)
            {
                TempData["FlashError"] = "We could not find that event.";
                return RedirectToAction("Events");
            }

            var e = result.Data;
            ViewData["Heading"] = "Edit event";
            return View("EventForm", new EventFormViewModel
            {
                Id = e.Id,
                Name = e.Name ?? "",
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location ?? "",
                EventType = e.EventType ?? "Run",
                DistanceKm = e.DistanceKm
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
        {
            ViewData["Heading"] = "Edit event";
            if (!ModelState.IsValid) return View("EventForm", model);

            var result = await _api.SendAsync(HttpMethod.Put, $"api/Events/{id}", ToPayload(model));
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                return View("EventForm", model);
            }

            TempData["Flash"] = "Event updated.";
            return RedirectToAction("Events");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var result = await _api.SendAsync(HttpMethod.Delete, $"api/Events/{id}");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();

            if (result.Success) TempData["Flash"] = "Event deleted.";
            else TempData["FlashError"] = result.Error;

            return RedirectToAction("Events");
        }

        // ---------- Choose an event (used by the menu) ----------

        public async Task<IActionResult> ChooseEvent(string target)
        {
            var result = await _api.GetAsync<List<EventDto>>("api/Events");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success) TempData["FlashError"] = result.Error;

            var userId = HttpContext.Session.GetInt32("UserId");
            var events = (result.Data ?? new List<EventDto>())
                .Where(e => e.OrganiserId == userId)
                .OrderBy(e => e.EventDate)
                .ToList();

            ViewBag.Target = target;
            return View(events);
        }

        // ---------- Categories ----------

        public async Task<IActionResult> Categories(int eventId)
        {
            var ev = await GetOwnedEventAsync(eventId);
            if (ev == null) return RedirectToAction("Events");

            var result = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success) TempData["FlashError"] = result.Error;

            ViewBag.Event = ev;
            return View(result.Data ?? new List<CategoryDto>());
        }

        [HttpGet]
        public async Task<IActionResult> CreateCategory(int eventId)
        {
            var ev = await GetOwnedEventAsync(eventId);
            if (ev == null) return RedirectToAction("Events");

            ViewData["Heading"] = "Add category";
            ViewBag.Event = ev;
            return View("CategoryForm", new CategoryFormViewModel { EventId = eventId, DistanceKm = ev.DistanceKm });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(int eventId, CategoryFormViewModel model)
        {
            var ev = await GetOwnedEventAsync(eventId);
            if (ev == null) return RedirectToAction("Events");

            ViewData["Heading"] = "Add category";
            ViewBag.Event = ev;
            model.EventId = eventId;
            if (!ModelState.IsValid) return View("CategoryForm", model);

            var result = await _api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/categories", CategoryPayload(model));
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                return View("CategoryForm", model);
            }

            TempData["Flash"] = "Category added.";
            return RedirectToAction("Categories", new { eventId });
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int eventId, int id)
        {
            var ev = await GetOwnedEventAsync(eventId);
            if (ev == null) return RedirectToAction("Events");

            var result = await _api.GetAsync<CategoryDto>($"api/events/{eventId}/categories/{id}");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success || result.Data == null)
            {
                TempData["FlashError"] = "We could not find that category.";
                return RedirectToAction("Categories", new { eventId });
            }

            var c = result.Data;
            ViewData["Heading"] = "Edit category";
            ViewBag.Event = ev;
            return View("CategoryForm", new CategoryFormViewModel
            {
                Id = c.Id,
                EventId = eventId,
                Name = c.Name ?? "",
                DistanceKm = c.DistanceKm,
                MaxParticipants = c.MaxParticipants,
                EntryFee = c.EntryFee
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int eventId, int id, CategoryFormViewModel model)
        {
            var ev = await GetOwnedEventAsync(eventId);
            if (ev == null) return RedirectToAction("Events");

            ViewData["Heading"] = "Edit category";
            ViewBag.Event = ev;
            model.EventId = eventId;
            model.Id = id;
            if (!ModelState.IsValid) return View("CategoryForm", model);

            var result = await _api.SendAsync(HttpMethod.Put, $"api/events/{eventId}/categories/{id}", CategoryPayload(model));
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                return View("CategoryForm", model);
            }

            TempData["Flash"] = "Category updated.";
            return RedirectToAction("Categories", new { eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int eventId, int id)
        {
            var result = await _api.SendAsync(HttpMethod.Delete, $"api/events/{eventId}/categories/{id}");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();

            if (result.Success) TempData["Flash"] = "Category deleted.";
            else TempData["FlashError"] = result.Error;

            return RedirectToAction("Categories", new { eventId });
        }

        // ---------- Enrolments ----------

        public async Task<IActionResult> Enrolments(int eventId)
        {
            var ev = await GetOwnedEventAsync(eventId);
            if (ev == null) return RedirectToAction("Events");

            var result = await _api.GetAsync<List<EventEnrolmentDto>>($"api/events/{eventId}/enrolments");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success) TempData["FlashError"] = result.Error;

            ViewBag.Event = ev;
            return View(result.Data ?? new List<EventEnrolmentDto>());
        }

        // ---------- Helpers ----------

        private static object ToPayload(EventFormViewModel m) => new
        {
            name = m.Name,
            description = m.Description,
            eventDate = m.EventDate,
            location = m.Location,
            eventType = m.EventType,
            distanceKm = m.DistanceKm
        };

        private IActionResult ExpiredSession()
        {
            HttpContext.Session.Clear();
            TempData["FlashError"] = "Your session has expired. Please log in again.";
            return RedirectToAction("Login", "Account");
        }
    }
}