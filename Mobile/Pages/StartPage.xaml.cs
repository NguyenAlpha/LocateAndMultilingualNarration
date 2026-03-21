using Mobile.ViewModels;

namespace Mobile;

public partial class StartPage : ContentPage
{
    public StartPage()
    {
        InitializeComponent();
        BindingContext = new StartViewModel();
    }
}
