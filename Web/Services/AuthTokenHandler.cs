using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Web.Services
{
    // AuthTokenHandler: DelegatingHandler dùng để tự động gắn header xác thực
    // và thông tin múi giờ vào mỗi request trước khi gửi đến API.
    public class AuthTokenHandler : DelegatingHandler
    {
        // Cho phép truy cập HttpContext/Session từ bên trong service không phải controller
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Constructor: nhận IHttpContextAccessor từ DI container
        public AuthTokenHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Can thiệp vào request trước khi gửi đi:
        // - lấy token từ Session
        // - thêm Authorization Bearer nếu có token
        // - thêm header X-TimeZoneId nếu client chưa gửi
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Lấy Session hiện tại; có thể null nếu không chạy trong HTTP request context
            var session = _httpContextAccessor.HttpContext?.Session;

            // Đọc token đã lưu khi đăng nhập
            var token = session?.GetString(ApiClient.TokenSessionKey);

            // Nếu có token thì gắn vào header Authorization
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Nếu API cần múi giờ, thêm header này khi chưa có
            if (!request.Headers.Contains("X-TimeZoneId"))
            {
                request.Headers.Add("X-TimeZoneId", TimeZoneInfo.Local.Id);
            }

            // Chuyển request xuống handler tiếp theo / HttpClient thực thi
            return base.SendAsync(request, cancellationToken);
        }
    }
}
