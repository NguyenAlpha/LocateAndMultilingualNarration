using Mobile.Helpers;
using Mobile.ViewModels;

namespace Mobile;

public partial class StartPage : ContentPage
{
    private readonly StartViewModel _viewModel;
    private bool _initialized;

    public StartPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<StartViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_initialized) return;
        _initialized = true;
        await _viewModel.InitializeAsync();
    }
}
