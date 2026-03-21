using Mobile.Pages;
namespace Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(ScanPage), typeof(ScanPage));
            Routing.RegisterRoute(nameof(LanguagePage), typeof(LanguagePage));
            Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
        }
    }
}
