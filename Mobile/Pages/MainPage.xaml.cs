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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadUserName();   // giữ nguyên
            // Nếu có LoadFeaturedStalls thì gọi thêm
            // _viewModel.LoadFeaturedStalls();
        }
    }
}