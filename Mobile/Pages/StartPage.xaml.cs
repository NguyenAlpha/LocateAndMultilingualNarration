namespace Mobile;

public partial class StartPage : ContentPage
{
    public StartPage()
    {
        InitializeComponent();
    }

    private async void OnScanQRClicked(object sender, EventArgs e)
    {
        // Tạm thời giả lập guest
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        //await Shell.Current.GoToAsync(nameof(LoginPage));
    }

}