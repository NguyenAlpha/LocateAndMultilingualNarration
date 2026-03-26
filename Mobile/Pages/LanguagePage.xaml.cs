using Mobile.Helpers;
using Mobile.ViewModels;

namespace Mobile;

public partial class LanguagePage : ContentPage
{
    private readonly LanguageViewModel _viewModel;

    public LanguagePage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<LanguageViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadLanguagesAsync();
    }
}