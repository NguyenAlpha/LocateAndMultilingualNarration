using Mobile.Helpers;

namespace Mobile;

public partial class LanguagePage : ContentPage
{
    public LanguagePage()
    {
        InitializeComponent();
    }

    async void SelectVietnamese(object sender, EventArgs e)
    {
        LanguageHelper.SetLanguage("vi");
        await ReloadApp();
    }

    async void SelectEnglish(object sender, EventArgs e)
    {
        LanguageHelper.SetLanguage("en");
        await ReloadApp();
    }

    async void SelectJapanese(object sender, EventArgs e)
    {
        LanguageHelper.SetLanguage("ja");
        await ReloadApp();
    }

    async void SelectKorean(object sender, EventArgs e)
    {
        LanguageHelper.SetLanguage("ko");
        await ReloadApp();
    }

    async Task ReloadApp()
    {
        await Shell.Current.DisplayAlert("Language", "Language changed successfully", "OK");

        Application.Current.MainPage = new AppShell();
    }
}