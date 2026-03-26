using Mobile.Helpers;
using Mobile.ViewModels;

namespace Mobile.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.GetService<LoginViewModel>();
    }

    // OLD CODE (kept for reference)
    // private async void OnLoginSuccess(object sender, EventArgs e)
    // {
    //     await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
    // }
}
