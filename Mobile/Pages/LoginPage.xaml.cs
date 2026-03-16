namespace Mobile;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
      InitializeComponent();
     BindingContext = Mobile.MauiProgram.Services?.GetService(typeof(Mobile.ViewModels.LoginViewModel)) as Mobile.ViewModels.LoginViewModel;
	}
}