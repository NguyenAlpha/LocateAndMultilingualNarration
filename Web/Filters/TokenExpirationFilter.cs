using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Services;

namespace Web.Filters
{
    public class TokenExpirationFilter : IAsyncActionFilter
    {
        private readonly ApiClient _apiClient;

        private static readonly string[] _publicPaths =
        [
            "/Auth/Login",
            "/Auth/Register",
            "/Auth/Logout",
            "/Home/Index",
            "/Home/Privacy",
            "/Subscription/Plans",
            "/Subscription/Success",
            "/",
        ];

        public TokenExpirationFilter(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var path = context.HttpContext.Request.Path;

            if (_publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
            {
                await next();
                return;
            }

            var session = context.HttpContext.Session;
            var token = session.GetString(ApiClient.TokenSessionKey);
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

            if (string.IsNullOrWhiteSpace(token))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
                return;
            }

            var expiresAtValue = session.GetString(ApiClient.TokenExpiresAtSessionKey);
            if (DateTimeOffset.TryParse(expiresAtValue, out var expiresAt) && expiresAt <= DateTimeOffset.UtcNow)
            {
                var refreshed = await _apiClient.RefreshAsync();
                if (!refreshed)
                {
                    session.Clear();
                    context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
                    return;
                }
            }

            if (path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                var role = session.GetString(ApiClient.UserRoleSessionKey);
                if (role != "Admin")
                {
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }
            }

            await next();
        }
    }
}
