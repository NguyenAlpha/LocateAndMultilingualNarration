using Mobile.Services;

namespace Mobile
{
    public partial class App : Application
    {
        private readonly ISyncBackgroundService _syncBackgroundService;

        public App(ISyncBackgroundService syncBackgroundService)
        {
            InitializeComponent();
            _syncBackgroundService = syncBackgroundService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override void OnSleep()
        {
            _syncBackgroundService.Stop();
        }

        protected override void OnResume()
        {
            _syncBackgroundService.Start();
        }
    }
}
