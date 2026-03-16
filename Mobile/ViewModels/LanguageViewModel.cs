using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Mobile.Helpers;

namespace Mobile.ViewModels
{
    public class LanguageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        string? _selectedDisplay;

        public string? SelectedDisplay
        {
            get => _selectedDisplay;
            set
            {
                if (_selectedDisplay == value) return;
                _selectedDisplay = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectLanguageCommand { get; }

        public LanguageViewModel()
        {
            SelectedDisplay = LanguageHelper.GetLanguageDisplay();
            SelectLanguageCommand = new Command<string?>(OnSelectLanguage);
        }

        void OnSelectLanguage(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;

            LanguageHelper.SetLanguage(code);
            SelectedDisplay = LanguageHelper.GetLanguageDisplay();

            // Navigate back to previous page
            try
            {
                Shell.Current.GoToAsync("..");
            }
            catch
            {
                // ignore navigation errors in ViewModel
            }
        }

        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
