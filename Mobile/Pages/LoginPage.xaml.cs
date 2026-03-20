namespace Mobile.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
		BindingContext = new Mobile.ViewModels.LoginViewModel();
	}
	private async void OnLoginSuccess(object sender, EventArgs e)
{
    await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
}

}
