using Mobile.ViewModels;

namespace Mobile;

public partial class LanguagePage : ContentPage
{
    private readonly LanguageViewModel _viewModel;

    public LanguagePage(LanguageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadLanguagesAsync();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchText = e.NewTextValue ?? string.Empty;
    }
}
