using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Services;

namespace Web.Filters
{
    public class TokenExpirationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var path = context.HttpContext.Request.Path;
            if (path.StartsWithSegments("/Auth/Login") || path.StartsWithSegments("/Auth/Logout"))
            {
                await next();
                return;
            }

            var session = context.HttpContext.Session;
            var expiresAtValue = session.GetString(ApiClient.TokenExpiresAtSessionKey);

            if (!string.IsNullOrWhiteSpace(expiresAtValue) && DateTimeOffset.TryParse(expiresAtValue, out var expiresAt))
            {
                if (expiresAt <= DateTimeOffset.Now)
                {
                    session.Clear();
                    context.Result = new RedirectToActionResult("Login", "Auth", null);
                    return;
                }
            }

            await next();
        }
    }
}
