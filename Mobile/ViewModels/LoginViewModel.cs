using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Mobile.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        string? _email;
        public string? Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        string? _password;
        public string? Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        public ICommand LoginCommand { get; }
        public ICommand GoToRegisterCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new Command(async () => await LoginAsync());
            GoToRegisterCommand = new Command(async () => await GoToRegisterAsync());
        }

        async Task LoginAsync()
        {
            // Mock login - always success
            Microsoft.Maui.Storage.Preferences.Set("jwt_token", "mock-token");
            App.Current.MainPage = new AppShell();
        }

        async Task GoToRegisterAsync()
        {
            await Shell.Current.GoToAsync("register");
        }

        void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
