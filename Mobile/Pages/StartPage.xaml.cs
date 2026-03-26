using Mobile.Helpers;
using Mobile.ViewModels;

namespace Mobile;

public partial class StartPage : ContentPage
{
    private readonly StartViewModel _viewModel;

    public StartPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<StartViewModel>();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
