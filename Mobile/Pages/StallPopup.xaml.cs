using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Mobile.ViewModels;
using Shared.DTOs.Geo;

namespace Mobile.Pages;

public partial class StallPopup : Popup
{
    private readonly MapViewModel _viewModel;
    private readonly ILogger<StallPopup> _logger;
    private GeoStallDto? _stall;

    public StallPopup(MapViewModel viewModel, ILogger<StallPopup> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;

        var info = DeviceDisplay.Current.MainDisplayInfo;
        var screenW = info.Width / info.Density;
        var screenH = info.Height / info.Density;
        WidthRequest = screenW;
        HeightRequest = screenH * 2 / 3;
        Margin = 0;
    }

    public void Init(GeoStallDto stall)
    {
        _stall = stall;
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

        if (stall.MediaImages.Count > 0)
        {
            ImageCarousel.ItemsSource = stall.MediaImages;
            ImageCarousel.IsVisible = true;
            ImageIndicator.IsVisible = stall.MediaImages.Count > 1;
        }
    }

    private async void OnPlayClicked(object? sender, EventArgs e)
    {
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("[Popup] Ấn Phát — StallName={StallName}, AudioUrl={AudioUrl}",
                _stall?.StallName ?? "(null)",
                _stall?.NarrationContent?.AudioUrl ?? "(null)");

        if (_stall is not null)
            _viewModel.PlayStall(_stall);

        await CloseAsync();
    }
}
