//using Mobile.ViewModels;

//namespace Mobile;

//public partial class BoothListPage : ContentPage
//{
//    BoothListViewModel vm;
//    public BoothListPage()
//    {
//        InitializeComponent();
//        vm = Mobile.MauiProgram.Services?.GetService(typeof(BoothListViewModel)) as BoothListViewModel;
//        BindingContext = vm;
//    }

//    protected override void OnAppearing()
//    {
//        base.OnAppearing();
//        vm.LoadCommand.Execute(null);
//    }

//    private void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//    {
//        var item = e.CurrentSelection.FirstOrDefault() as Mobile.Models.Booth;
//        if (item != null)
//        {
//            vm.SelectCommand.Execute(item);
//        }
//    }
//}
