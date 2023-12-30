using Microsoft.UI.Xaml.Controls;

using RyTuneX.ViewModels;

namespace RyTuneX.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel
    {
        get;
    }

    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
    }
}
