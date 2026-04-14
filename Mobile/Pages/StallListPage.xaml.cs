using Mobile.Helpers;
using Mobile.ViewModels;

namespace Mobile.Pages;

public partial class StallListPage : ContentPage
{
    public StallListPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.GetService<StallListViewModel>();
    }
}