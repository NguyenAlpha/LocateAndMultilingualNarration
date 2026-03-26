using Microsoft.Maui.Storage;

namespace Mobile.Services;

public class SessionService
{
    private const string TokenKey = "token";
    private const string UserNameKey = "username";
    private const string GuestModeKey = "guest_mode";

    public void SaveSession(string token, string userName)
    {
        Preferences.Set(TokenKey, token);
        Preferences.Set(UserNameKey, userName);
        Preferences.Set(GuestModeKey, false);
    }

    public bool IsLoggedIn() => !string.IsNullOrWhiteSpace(Preferences.Get(TokenKey, string.Empty));

    public string GetToken() => Preferences.Get(TokenKey, string.Empty);

    public string GetUserName() => Preferences.Get(UserNameKey, "Guest");

    public void SetGuestMode(bool isGuest)
    {
        Preferences.Set(GuestModeKey, isGuest);
    }

    public void ClearSession()
    {
        Preferences.Clear();
    }

    // OLD CODE (kept for reference)
    // public void SetGuestMode(bool isGuest)
    // {
    //     Preferences.Set("guest_mode", isGuest);
    // }
    //
    // public void ClearSession()
    // {
    //     Preferences.Remove("jwt_token");
    //     Preferences.Remove("guest_mode");
    // }
}
