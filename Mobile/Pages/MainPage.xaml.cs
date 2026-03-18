using Microsoft.Maui.Controls;
using Mobile.Helpers;
using Mobile.Pages;
using System;

namespace Mobile
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Ensure the language label has a fallback value so the UI is never empty
            try
            {
                if (lblLanguageDisplay != null)
                {
                    lblLanguageDisplay.Text = LanguageHelper.GetLanguageDisplay() ?? "Ngôn ngữ: Tiếng Việt";
                }
            }
            catch
            {
                // ignore any errors updating the UI
            }
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            // Navigate to LanguagePage when the start button is clicked
            await Shell.Current.GoToAsync(nameof(LanguagePage));
        }
        private bool isAudioOn = false;

        private async void OnMapClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(MapPage));
        }

        private async void OnLanguageClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(LanguagePage));
        }

        private async void OnAudioClicked(object sender, EventArgs e)
        {
            isAudioOn = !isAudioOn;

            btnAudio.Text = isAudioOn ? "🔇 Tắt Audio" : "🔊 Audio";

            await DisplayAlert("Audio",
                isAudioOn ? "Đã bật thuyết minh" : "Đã tắt thuyết minh", "OK");
        }
    }
}
