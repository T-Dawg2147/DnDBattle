using DnDBattle.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DnDBattle.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }
}
