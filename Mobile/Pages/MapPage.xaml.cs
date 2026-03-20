using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Mobile.Models;
using Mobile.Services;
using System.Linq;
namespace Mobile.Pages;

public partial class MapPage : ContentPage
{
    public MapPage()
    {
        InitializeComponent();

        // Load mock data and bind to CollectionView
        var stalls = MockDataService.GetStalls();
        //StallCollection.ItemsSource = stalls;

        //StallCollection.SelectionChanged += StallCollection_SelectionChanged;
    }

    private async void StallCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var item = e.CurrentSelection.FirstOrDefault() as Booth;
        if (item != null)
        {
            // For now show detail in an alert (StallDetailPage not implemented)
            await Shell.Current.DisplayAlert(item.Name, item.Description, "OK");
        }
    }
    //private async void OnRefreshClicked(object sender, EventArgs e)
    //{
    //    var location = await Geolocation.GetLocationAsync();

    //    if (location != null)
    //    {
    //        var pos = new Location(location.Latitude, location.Longitude);

    //        map.MoveToRegion(MapSpan.FromCenterAndRadius(
    //            pos,
    //            Distance.FromKilometers(1)));

    //        map.Pins.Clear();

    //        map.Pins.Add(new Pin
    //        {
    //            Label = "Bạn đang ở đây",
    //            Location = pos
    //        });
    //    }
  }
