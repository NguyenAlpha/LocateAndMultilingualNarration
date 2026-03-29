using CommunityToolkit.Maui.Views;
using Mobile.ViewModels;
using Shared.DTOs.Geo;

namespace Mobile.Pages;

public partial class StallPopup : Popup
{
    private readonly MapViewModel _viewModel;

    public StallPopup(MapViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    public void Init(GeoStallDto stall)
    {
        StallNameLabel.Text = stall.StallName;
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        _viewModel.PlayAudioCommand.Execute(null);
    }
}
