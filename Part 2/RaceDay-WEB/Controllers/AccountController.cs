using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay_WEB.Services;

namespace RaceDay.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApiService _api;

        public AccountController(ApiService api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Token")))
                return RedirectToAction("Index", "Home");
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var token = await _api.LoginAsync(model.Email, model.Password);
                if (token == null)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                var profile = await _api.GetProfileAsync(token);
                if (profile == null)
                {
                    ModelState.AddModelError("", "We could not load your profile. Please try again.");
                    return View(model);
                }

                HttpContext.Session.SetString("Token", token);
                HttpContext.Session.SetInt32("UserId", profile.UserId);
                HttpContext.Session.SetString("Role", profile.Role ?? "");
                HttpContext.Session.SetString("FullName", profile.FullName ?? "");

                // Role-specific landing pages are added in Step 3.
                if (string.Equals(profile.Role, "Organiser", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Dashboard", "Organiser");

                return RedirectToAction("Home", "Participant");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "The service is unavailable right now. Please try again shortly.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var (ok, error) = await _api.RegisterAsync(model);
                if (!ok)
                {
                    ModelState.AddModelError("", "Registration failed. " + error);
                    return View(model);
                }
                TempData["Success"] = "Account created! Please log in.";
                return RedirectToAction("Login");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("", "The service is unavailable right now. Please try again shortly.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}