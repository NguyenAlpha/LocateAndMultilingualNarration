using Mobile.ViewModels;

namespace Mobile.Pages;

[QueryProperty(nameof(TourId), "tourId")]
public partial class TourDetailPage : ContentPage
{
    private readonly TourDetailViewModel _viewModel;

    public string? TourId { get; set; }

    public TourDetailPage(TourDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Guid.TryParse(TourId, out var id) && id != _viewModel.TourId)
        {
            _viewModel.TourId = id;
        }

        if (_viewModel.TourId != Guid.Empty)
        {
            await _viewModel.LoadAsync();
        }
    }
}
