using Mobile.ViewModels;

namespace Mobile.Pages;

public partial class StallListPage : ContentPage
{
    public StallListPage(StallListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}