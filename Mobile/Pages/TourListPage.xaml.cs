using Mobile.ViewModels;

namespace Mobile.Pages;

public partial class TourListPage : ContentPage
{
    private readonly TourListViewModel _viewModel;

    public TourListPage(TourListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadToursAsync();
    }
}
