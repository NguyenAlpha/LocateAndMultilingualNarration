using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Services;

namespace Web.Filters
{
    public class TokenExpirationFilter : IAsyncActionFilter
    {
        private static readonly string[] _publicPaths =
        [
            "/Auth/Login",
            "/Auth/Register",
            "/Auth/Logout",
            "/Home/Index",
            "/Home/Privacy",
        ];

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var path = context.HttpContext.Request.Path;

            // Bỏ qua các trang public
            if (_publicPaths.Any(p => path.StartsWithSegments(p)))
            {
                await next();
                return;
            }

            var session = context.HttpContext.Session;
            var token = session.GetString(ApiClient.TokenSessionKey);

            // Chưa đăng nhập → redirect Login
            if (string.IsNullOrWhiteSpace(token))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Token hết hạn → xóa session, redirect Login
            var expiresAtValue = session.GetString(ApiClient.TokenExpiresAtSessionKey);
            if (DateTimeOffset.TryParse(expiresAtValue, out var expiresAt) && expiresAt <= DateTimeOffset.Now)
            {
                session.Clear();
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            await next();
        }
    }
}
