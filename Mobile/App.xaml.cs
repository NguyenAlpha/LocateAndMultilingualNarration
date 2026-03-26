using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Mobile.Services;

namespace Mobile
{
    public partial class App : Application
    {
        private readonly SessionService _sessionService;

        public App(SessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            window.Created += async (_, _) =>
            {
                if (_sessionService.IsLoggedIn())
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Shell.Current.GoToAsync("//MainPage");
                    });
                }
            };

            return window;
        }
    }
}