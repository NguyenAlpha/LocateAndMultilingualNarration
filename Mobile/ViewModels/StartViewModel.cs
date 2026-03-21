using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Pages;

namespace Mobile.ViewModels;

public class StartViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ScanCommand { get; }
    public ICommand LoginCommand { get; }

    public StartViewModel()
    {
        ScanCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(ScanPage)));
        LoginCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(LoginPage)));
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
