namespace Mobile;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
		BindingContext = new Mobile.ViewModels.LoginViewModel();
	}
}
