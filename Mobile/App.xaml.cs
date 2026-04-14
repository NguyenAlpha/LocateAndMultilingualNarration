using Mobile.Services;

namespace Mobile
{
    public partial class App : Application
    {
        private readonly ISyncBackgroundService _syncBackgroundService;
        private readonly ILocationLogService _locationLogService;

        public App(ISyncBackgroundService syncBackgroundService, ILocationLogService locationLogService)
        {
            InitializeComponent();
            _syncBackgroundService = syncBackgroundService;
            _locationLogService = locationLogService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override void OnSleep()
        {
            _ = _locationLogService.FlushAsync();
            _syncBackgroundService.Stop();
        }

        protected override void OnResume()
        {
            _syncBackgroundService.Start();
        }
    }
}
