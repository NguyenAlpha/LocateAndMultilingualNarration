using Microsoft.Maui.Storage;

namespace Mobile.Services;

public class SessionService
{
    public void SetGuestMode(bool isGuest)
    {
        Preferences.Set("guest_mode", isGuest);
    }

    public void ClearSession()
    {
        Preferences.Remove("jwt_token");
        Preferences.Remove("guest_mode");
    }
}
