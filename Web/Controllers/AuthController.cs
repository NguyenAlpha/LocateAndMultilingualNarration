using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Auth;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly BusinessApiClient _businessApiClient;

        public AuthController(ApiClient apiClient, BusinessApiClient businessApiClient)
        {
            _apiClient = apiClient;
            _businessApiClient = businessApiClient;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _apiClient.LoginAsync(new LoginRequestDto
            {
                Email = model.Email,
                Password = model.Password
            }, cancellationToken);

            if (result?.Success != true || result.Data == null)
            {
                ModelState.AddModelError(string.Empty, result?.Error?.Message ?? "Đăng nhập thất bại.");
                return View(model);
            }

            _apiClient.StoreToken(result.Data);

            // Nếu là BusinessOwner thì lấy plan của business đầu tiên để hiển thị trên sidebar
            var role = result.Data.Roles.FirstOrDefault() ?? string.Empty;
            if (role == "BusinessOwner")
            {
                var businesses = await _businessApiClient.GetBusinessesAsync(1, 1, null, cancellationToken);
                var firstBusiness = businesses?.Data?.Items?.FirstOrDefault();
                _apiClient.StoreUserPlan(firstBusiness?.Plan ?? "Free", firstBusiness?.PlanExpiresAt);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _apiClient.RegisterBusinessOwnerAsync(new RegisterBusinessOwnerDto
            {
                UserName = model.UserName,
                Email = model.Email,
                Password = model.Password,
                PhoneNumber = model.PhoneNumber
            }, cancellationToken);

            if (result?.Success != true)
            {
                ModelState.AddModelError(string.Empty, result?.Error?.Message ?? "Đăng ký thất bại.");
                return View(model);
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            _apiClient.ClearToken();
            return RedirectToAction("Login");
        }
    }
}
