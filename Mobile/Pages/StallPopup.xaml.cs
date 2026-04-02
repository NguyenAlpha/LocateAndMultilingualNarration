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

        // Chiếm 100% chiều rộng, 2/3 chiều cao màn hình
        var density = DeviceDisplay.Current.MainDisplayInfo.Density;
        var screenH = DeviceDisplay.Current.MainDisplayInfo.Height / density;
        WidthRequest = -1; // -1 = fill available width
        HeightRequest = screenH * 2 / 3;
    }

    public void Init(GeoStallDto stall)
    {
        StallNameLabel.Text = stall.StallName;

        var narration = stall.NarrationContent;
        if (narration != null)
        {
            if (!string.IsNullOrWhiteSpace(narration.Description))
            {
                DescriptionLabel.Text = narration.Description;
                DescriptionLabel.IsVisible = true;
            }
            ScriptTextLabel.Text = narration.ScriptText;
        }
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        _viewModel.PlayAudioCommand.Execute(null);
    }
}
