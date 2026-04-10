using Mobile.ViewModels;

namespace Mobile;

//[QueryProperty(nameof(StallId), "stallId")]
[QueryProperty(nameof(Token), "token")]
public partial class LanguagePage : ContentPage
{
    private readonly LanguageSelectionViewModel _viewModel;

    public string? StallId { get; set; }
    public string? Token { get; set; }

    public LanguagePage(LanguageSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.SetScanContext(StallId, Token);
        await _viewModel.LoadLanguagesAsync();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // Logic search đã được xử lý trong ViewModel
    }
}