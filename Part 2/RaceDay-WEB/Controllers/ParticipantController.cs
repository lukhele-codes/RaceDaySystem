using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay_WEB.Models;
using RaceDay_WEB.Services;
using System.Net;

namespace RaceDay_WEB.Controllers
{
    [SessionAuthorize("Participant")]
    public class ParticipantController : Controller
    {
        private readonly ApiService _api;

        public ParticipantController(ApiService api)
        {
            _api = api;
        }

        // ---------- Home ----------

        public async Task<IActionResult> Home()
        {
            var events = await _api.GetAsync<List<EventDto>>("api/Events");
            if (events.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();

            var mine = await _api.GetAsync<List<EnrolmentDto>>("api/Enrolments/mine");
            var results = await _api.GetAsync<List<ResultDto>>("api/results/mine");

            var myList = mine.Data ?? new List<EnrolmentDto>();
            var model = new ParticipantHomeViewModel
            {
                Upcoming = (events.Data ?? new List<EventDto>())
                    .Where(e => e.EventDate >= DateTime.Now)
                    .OrderBy(e => e.EventDate)
                    .Take(3)
                    .ToList(),
                MyEnrolments = myList.Count,
                Confirmed = myList.Count(x => (x.Status ?? "").ToLower().Contains("confirm")),
                ResultsCount = results.Data?.Count ?? 0
            };
            return View(model);
        }

        // ---------- Browse events ----------

        public async Task<IActionResult> Events(string? search, string? type)
        {
            var url = "api/Events";
            if (!string.IsNullOrWhiteSpace(search))
                url += "?search=" + Uri.EscapeDataString(search);

            var result = await _api.GetAsync<List<EventDto>>(url);
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success) TempData["FlashError"] = result.Error;

            var events = (result.Data ?? new List<EventDto>())
                .Where(e => e.EventDate >= DateTime.Today);

            if (!string.IsNullOrWhiteSpace(type))
                events = events.Where(e => string.Equals(e.EventType, type, StringComparison.OrdinalIgnoreCase));

            ViewBag.Search = search;
            ViewBag.Type = type;
            return View(events.OrderBy(e => e.EventDate).ToList());
        }

        // ---------- Event detail and entry ----------

        public async Task<IActionResult> EventDetail(int id)
        {
            var ev = await _api.GetAsync<EventDto>($"api/Events/{id}");
            if (ev.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!ev.Success || ev.Data == null)
            {
                TempData["FlashError"] = "We could not find that event.";
                return RedirectToAction("Events");
            }

            var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
            var mine = await _api.GetAsync<List<EnrolmentDto>>("api/Enrolments/mine");

            var model = new EventDetailViewModel
            {
                Event = ev.Data,
                Categories = cats.Data ?? new List<CategoryDto>(),
                MyEnrolment = (mine.Data ?? new List<EnrolmentDto>())
                    .FirstOrDefault(m => m.EventId == id && !(m.Status ?? "").ToLower().Contains("cancel"))
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterEvent(int eventId, int categoryId)
        {
            var result = await _api.SendAsync(HttpMethod.Post, "api/Enrolments", new { categoryId });
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();

            if (!result.Success)
            {
                TempData["FlashError"] = "We could not complete your entry. The category may be full or you may already be entered.";
                return RedirectToAction("EventDetail", new { id = eventId });
            }

            TempData["Flash"] = "You are entered! See your entry below.";
            return RedirectToAction("MyEvents");
        }

        // ---------- My events ----------

        public async Task<IActionResult> MyEvents()
        {
            var mine = await _api.GetAsync<List<EnrolmentDto>>("api/Enrolments/mine");
            if (mine.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!mine.Success) TempData["FlashError"] = mine.Error;

            var events = await _api.GetAsync<List<EventDto>>("api/Events");
            var lookup = (events.Data ?? new List<EventDto>()).ToDictionary(e => e.Id);

            var rows = (mine.Data ?? new List<EnrolmentDto>())
                .Select(m => new MyEnrolmentRow
                {
                    Enrolment = m,
                    Event = lookup.TryGetValue(m.EventId, out var ev) ? ev : null
                })
                .OrderByDescending(r => r.Event?.EventDate)
                .ToList();

            return View(rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var result = await _api.SendAsync(HttpMethod.Delete, $"api/Enrolments/{id}");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();

            if (result.Success) TempData["Flash"] = "Your entry has been cancelled.";
            else TempData["FlashError"] = result.Error;

            return RedirectToAction("MyEvents");
        }

        // ---------- My results ----------

        public async Task<IActionResult> MyResults()
        {
            var results = await _api.GetAsync<List<ResultDto>>("api/results/mine");
            if (results.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!results.Success) TempData["FlashError"] = results.Error;

            var mine = await _api.GetAsync<List<EnrolmentDto>>("api/Enrolments/mine");
            var events = await _api.GetAsync<List<EventDto>>("api/Events");

            var enrolments = (mine.Data ?? new List<EnrolmentDto>()).ToDictionary(e => e.Id);
            var eventLookup = (events.Data ?? new List<EventDto>()).ToDictionary(e => e.Id);

            var rows = (results.Data ?? new List<ResultDto>())
                .Select(r =>
                {
                    EventDto? ev = null;
                    if (enrolments.TryGetValue(r.EnrolmentId, out var en))
                        eventLookup.TryGetValue(en.EventId, out ev);
                    return new MyResultRow { Result = r, Event = ev };
                })
                .OrderByDescending(r => r.Event?.EventDate)
                .ToList();

            return View(rows);
        }

        // ---------- Profile ----------

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var result = await _api.GetAsync<UserProfile>("api/users/me");
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success || result.Data == null)
            {
                TempData["FlashError"] = "We could not load your profile.";
                return RedirectToAction("Home");
            }

            return View(new ProfileViewModel
            {
                FullName = result.Data.FullName ?? "",
                Email = result.Data.Email ?? "",
                Role = result.Data.Role
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // The role is sent back unchanged so participants cannot change it here.
            var current = await _api.GetAsync<UserProfile>("api/users/me");
            var role = current.Data?.Role;

            var result = await _api.SendAsync(HttpMethod.Put, "api/users/me",
                new { fullName = model.FullName, email = model.Email, role });
            if (result.StatusCode == HttpStatusCode.Unauthorized) return ExpiredSession();
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Error);
                return View(model);
            }

            HttpContext.Session.SetString("FullName", model.FullName);
            TempData["Flash"] = "Profile updated.";
            return RedirectToAction("Profile");
        }

        private IActionResult ExpiredSession()
        {
            HttpContext.Session.Clear();
            TempData["FlashError"] = "Your session has expired. Please log in again.";
            return RedirectToAction("Login", "Account");
        }
    }
}