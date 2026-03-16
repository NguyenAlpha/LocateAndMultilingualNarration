using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Services;
using Microsoft.Maui.Controls;

namespace Mobile.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        readonly ApiService _api;
        public event PropertyChangedEventHandler? PropertyChanged;

        string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        string _password;
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        public ICommand LoginCommand { get; }
        public ICommand GoToRegisterCommand { get; }

        public LoginViewModel(ApiService api)
        {
            _api = api;
            LoginCommand = new Command(async () => await LoginAsync());
            GoToRegisterCommand = new Command(async () => await GoToRegisterAsync());
        }

        async Task LoginAsync()
        {
            var res = await _api.LoginAsync(Email, Password);
            if (res.Success)
            {
                // Login success - switch to main AppShell with all features
                Microsoft.Maui.Storage.Preferences.Set("jwt_token", res.Token ?? "");
                App.Current.MainPage = new AppShell();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Login failed", "Invalid credentials", "OK");
            }
        }

        async Task GoToRegisterAsync()
        {
            await Shell.Current.GoToAsync(nameof(Mobile.RegisterPage));
        }

        void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
