namespace Mobile.Components;

public partial class HeaderView : Grid
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(HeaderView), string.Empty);

    public static readonly BindableProperty LogoutCommandProperty =
        BindableProperty.Create(nameof(LogoutCommand), typeof(System.Windows.Input.ICommand), typeof(HeaderView));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public System.Windows.Input.ICommand? LogoutCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(LogoutCommandProperty);
        set => SetValue(LogoutCommandProperty, value);
    }

    public HeaderView()
    {
        InitializeComponent();
        BindingContext = this;
    }
}