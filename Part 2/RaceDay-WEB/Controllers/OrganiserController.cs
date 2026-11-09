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

        public async Task<IActionResult> Dashboard()
        {
            var result = await _api.GetAsync<List<EventDto>>("api/Events");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success) TempData["FlashError"] = result.Error;

            var userId = HttpContext.Session.GetInt32("UserId");
            var events = (result.Data ?? new List<EventDto>())
                .Where(e => e.OrganiserId == userId)
                .OrderBy(e => e.EventDate)
                .ToList();

            var enrolments = new List<EventEnrolmentDto>();
            foreach (var e in events)
            {
                var er = await _api.GetAsync<List<EventEnrolmentDto>>($"api/events/{e.Id}/enrolments");
                if (er.Success && er.Data != null) enrolments.AddRange(er.Data);
            }

            var model = new DashboardViewModel
            {
                TotalEvents = events.Count,
                UpcomingEvents = events.Count(e => e.EventDate >= DateTime.Now),
                TotalEnrolments = enrolments.Count,
                ConfirmedEnrolments = enrolments.Count(x => (x.Status ?? "").ToLower().Contains("confirm")),
                PendingEnrolments = enrolments.Count(x => (x.Status ?? "").ToLower().Contains("pend")),
                NextEvents = events.Where(e => e.EventDate >= DateTime.Now).Take(3).ToList()
            };
            return View(model);
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

        // ---------- Results ----------

        public async Task<IActionResult> Results(int eventId, int? categoryId)
        {
            var ev = await GetOwnedEventAsync(eventId);
            if (ev == null) return RedirectToAction("Events");

            var catResult = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
            if (catResult.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!catResult.Success) TempData["FlashError"] = catResult.Error;

            var categories = catResult.Data ?? new List<CategoryDto>();
            var selected = categories.FirstOrDefault(c => c.Id == categoryId) ?? categories.FirstOrDefault();

            var rows = new List<ResultRowViewModel>();
            if (selected != null)
            {
                var enrolResult = await _api.GetAsync<List<EventEnrolmentDto>>($"api/events/{eventId}/enrolments");
                var resResult = await _api.GetAsync<List<ResultDto>>($"api/categories/{selected.Id}/results");
                if (enrolResult.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
                if (!enrolResult.Success) TempData["FlashError"] = enrolResult.Error;

                var results = resResult.Data ?? new List<ResultDto>();
                rows = (enrolResult.Data ?? new List<EventEnrolmentDto>())
                    .Where(e => e.CategoryId == selected.Id)
                    .Select(e => new ResultRowViewModel
                    {
                        Enrolment = e,
                        Result = results.FirstOrDefault(r => r.EnrolmentId == e.Id)
                    })
                    .OrderBy(r => r.Result?.Position ?? int.MaxValue)
                    .ThenBy(r => r.Enrolment.ParticipantName)
                    .ToList();
            }

            ViewBag.Event = ev;
            ViewBag.Categories = categories;
            ViewBag.Selected = selected;
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> CaptureResult(int eventId, int enrolmentId, int? resultId)
        {
            var ev = await GetOwnedEventAsync(eventId);
            if (ev == null) return RedirectToAction("Events");

            var enrolResult = await _api.GetAsync<List<EventEnrolmentDto>>($"api/events/{eventId}/enrolments");
            if (enrolResult.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();

            var enrolment = enrolResult.Data?.FirstOrDefault(e => e.Id == enrolmentId);
            if (enrolment == null)
            {
                TempData["FlashError"] = "We could not find that enrolment.";
                return RedirectToAction("Results", new { eventId });
            }

            var model = new ResultFormViewModel
            {
                EventId = eventId,
                CategoryId = enrolment.CategoryId,
                EnrolmentId = enrolmentId,
                ResultId = resultId,
                ParticipantName = enrolment.ParticipantName,
                CategoryName = enrolment.CategoryName
            };

            if (resultId != null)
            {
                var resResult = await _api.GetAsync<List<ResultDto>>($"api/categories/{enrolment.CategoryId}/results");
                var existing = resResult.Data?.FirstOrDefault(r => r.Id == resultId);
                if (existing != null)
                {
                    model.Position = existing.Position;
                    if (TimeSpan.TryParse(existing.FinishTime, out var ts))
                        model.FinishTime = ts.ToString(@"hh\:mm\:ss");
                }
            }

            ViewBag.Event = ev;
            return View("ResultForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(ResultFormViewModel model)
        {
            var ev = await GetOwnedEventAsync(model.EventId);
            if (ev == null) return RedirectToAction("Events");
            ViewBag.Event = ev;

            if (!ModelState.IsValid) return View("ResultForm", model);

            if (string.IsNullOrWhiteSpace(model.FinishTime) && model.Position == null)
            {
                ModelState.AddModelError("", "Please enter a finish time, a position, or both.");
                return View("ResultForm", model);
            }

            string? finishTime = string.IsNullOrWhiteSpace(model.FinishTime)
                ? null
                : TimeSpan.Parse(model.FinishTime).ToString();

            ApiResult result;
            if (model.ResultId == null)
            {
                result = await _api.SendAsync(HttpMethod.Post, "api/results",
                    new { finishTime, position = model.Position, enrolmentId = model.EnrolmentId });
            }
            else
            {
                result = await _api.SendAsync(HttpMethod.Put, $"api/results/{model.ResultId}",
                    new { finishTime, position = model.Position });
            }

            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                return View("ResultForm", model);
            }

            TempData["Flash"] = "Result saved.";
            return RedirectToAction("Results", new { eventId = model.EventId, categoryId = model.CategoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResult(int eventId, int categoryId, int id)
        {
            var result = await _api.SendAsync(HttpMethod.Delete, $"api/results/{id}");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();

            if (result.Success) TempData["Flash"] = "Result removed.";
            else TempData["FlashError"] = result.Error;

            return RedirectToAction("Results", new { eventId, categoryId });
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

        private static object CategoryPayload(CategoryFormViewModel m) => new
        {
            name = m.Name,
            distanceKm = m.DistanceKm,
            maxParticipants = m.MaxParticipants,
            entryFee = m.EntryFee
        };

        // Loads an event and checks it belongs to the logged-in organiser.
        private async Task<EventDto?> GetOwnedEventAsync(int eventId)
        {
            var result = await _api.GetAsync<EventDto>($"api/Events/{eventId}");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (!result.Success || result.Data == null || result.Data.OrganiserId != userId)
            {
                TempData["FlashError"] = "We could not find that event.";
                return null;
            }
            return result.Data;
        }
    }
}