using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Filters
{
    // Put [SessionAuthorize] on a controller to require login.
    // Put [SessionAuthorize("Organiser")] to require a specific role.
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public SessionAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (string.IsNullOrEmpty(session.GetString("Token")))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var role = session.GetString("Role");
            if (_roles.Length > 0 && !_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }
}