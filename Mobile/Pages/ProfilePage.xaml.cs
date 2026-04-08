using Mobile.ViewModels;

namespace Mobile.Pages
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage(ProfileViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // OLD CODE (kept for reference): if (BindingContext is ProfileViewModel vm) await vm.LoadProfileAsync();
            if (BindingContext is ProfileViewModel vm)
            {
                await vm.LoadProfileAsync();
            }
        }
    }
}