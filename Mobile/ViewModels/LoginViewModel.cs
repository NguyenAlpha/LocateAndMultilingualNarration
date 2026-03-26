using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Mobile.Services;

namespace Mobile.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthService _authService;
        private readonly SessionService _sessionService;

        public event PropertyChangedEventHandler? PropertyChanged;

        string? _email;
        public string? Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        string? _password;
        public string? Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        string? _errorMessage;
        public string? ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }

        bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        public ICommand LoginCommand { get; }

        public ICommand GoToRegisterCommand { get; }

        public LoginViewModel(IAuthService authService, SessionService sessionService)
        {
            _authService = authService;
            _sessionService = sessionService;

            LoginCommand = new Command(async () => await LoginAsync());
            GoToRegisterCommand = new Command(async () => await GoToRegisterAsync());
        }

        async Task LoginAsync()
        {
            if (IsBusy)
            {
                return;
            }

            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Email và mật khẩu là bắt buộc.";
                return;
            }

            if (!Regex.IsMatch(Email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            {
                ErrorMessage = "Email không hợp lệ.";
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _authService.LoginAsync(Email.Trim(), Password);
                if (!result.IsSuccess)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _sessionService.SaveSession(result.Token, result.UserName);
                await Shell.Current.GoToAsync("//MainPage");
            }
            finally
            {
                IsBusy = false;
            }
        }

        async Task GoToRegisterAsync()
        {
            await Shell.Current.DisplayAlert("Info", "Register chưa được triển khai.", "OK");
        }

        void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
