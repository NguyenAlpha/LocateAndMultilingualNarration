using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Web.Services
{
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthTokenHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var token = session?.GetString(ApiClient.TokenSessionKey);

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (!request.Headers.Contains("X-TimeZoneId"))
            {
                request.Headers.Add("X-TimeZoneId", TimeZoneInfo.Local.Id);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
