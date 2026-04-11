using Mobile.Helpers;
using Mobile.ViewModels;

namespace Mobile
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = ServiceHelper.GetService<MainViewModel>();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_viewModel != null)
            {
                _viewModel.LoadUserName();

                // Load dữ liệu gian hàng
                await _viewModel.LoadFeaturedStallsAsync();
            }
        }
    }
}