using Microsoft.Maui.Controls;
using Mobile.Helpers;
using Mobile.Pages;
using System;

namespace Mobile
{
    public partial class MainPage : ContentPage
    {
        private bool isAudioOn = false;


        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Đảm bảo label ngôn ngữ luôn có giá trị mặc định để UI không bị trống
            try
            {
                if (lblLanguageDisplay != null)
                {
                    lblLanguageDisplay.Text = LanguageHelper.GetLanguageDisplay() ?? "Ngôn ngữ: Tiếng Việt";
                }
            }
            catch
            {
                // Bỏ qua lỗi cập nhật UI nếu có
            }
        }

        // ==========================================
        // 1. CÁC HÀM XỬ LÝ ĐIỀU HƯỚNG & LOGIC HIỆN TẠI CỦA BẠN
        // ==========================================

        private async void OnStartClicked(object sender, EventArgs e)
        {
            // Điều hướng sang LanguagePage khi bấm "Bắt đầu khám phá"
            await Shell.Current.GoToAsync(nameof(LanguagePage));
        }

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

            // Update the ImageButton's FontImageSource glyph to reflect audio state
            if (btnAudio?.Source is FontImageSource btnFont)
            {
                btnFont.Glyph = isAudioOn ? "🔇" : "🔊";
            }

            await DisplayAlert("Audio",
                isAudioOn ? "Đã bật thuyết minh" : "Đã tắt thuyết minh", "OK");
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            var selected = picker?.SelectedItem?.ToString();

            if (selected == null) return;

            if (lblLanguageDisplay != null)
            {
                lblLanguageDisplay.Text = "Ngôn ngữ: " + selected;
            }

            LanguageHelper.SetLanguage(selected);
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Profile", "Trang cá nhân", "OK");
        }

        // ==========================================
        // 2. CÁC HÀM XỬ LÝ MENU TRƯỢT (DRAWER) MỚI THÊM VÀO
        // ==========================================

        private async void OpenDrawer_Clicked(object sender, EventArgs e)
        {
            // Hiển thị lớp phủ và trượt menu từ trái sang
            if (DrawerOverlay == null || DrawerView == null)
                return;

            DrawerOverlay.IsVisible = true;
            await DrawerView.TranslateTo(0, 0, 250, Easing.CubicOut);
        }

        private async void CloseDrawer_Tapped(object sender, EventArgs e)
        {
            // Trượt menu ra ngoài và ẩn lớp phủ
            if (DrawerView == null || DrawerOverlay == null)
                return;

            await DrawerView.TranslateTo(-280, 0, 250, Easing.CubicIn);
            DrawerOverlay.IsVisible = false;
        }
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            // Đóng menu trước (nếu đang mở)
            DrawerOverlay.IsVisible = false;

            // Navigate về LoginPage
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
        }
    }
}