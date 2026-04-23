using Mobile.ViewModels;

namespace Mobile
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
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