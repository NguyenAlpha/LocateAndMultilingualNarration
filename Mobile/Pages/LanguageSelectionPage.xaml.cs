using Mobile.Helpers;
using Mobile.ViewModels;

namespace Mobile.Pages;

[QueryProperty(nameof(StallId), "stallId")]
[QueryProperty(nameof(Token), "token")]
public partial class LanguageSelectionPage : ContentPage
{
    private readonly LanguageSelectionViewModel _viewModel;
    private bool _isLoaded;

    public string? StallId { get; set; }
    public string? Token { get; set; }

    public LanguageSelectionPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<LanguageSelectionViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Nhận context stall/token từ bước scan để dùng khi điều hướng sang MapPage.
        _viewModel.SetScanContext(StallId, Token);

        if (_isLoaded)
        {
            return;
        }

        _isLoaded = true;
        await _viewModel.LoadLanguagesAsync();
    }
}
