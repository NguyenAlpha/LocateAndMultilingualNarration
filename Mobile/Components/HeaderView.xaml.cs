namespace Mobile.Components;

public partial class HeaderView : Grid
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(HeaderView), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public HeaderView()
    {
        InitializeComponent();
        BindingContext = this;
    }
}