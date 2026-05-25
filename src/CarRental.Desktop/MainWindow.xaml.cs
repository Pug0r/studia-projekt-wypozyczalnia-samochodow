using System;
using System.Windows;

namespace CarRental.Desktop;

public partial class MainWindow : Window
{
    public IServiceProvider Services => ((AppHost)Application.Current).Services;

    public MainWindow()
    {
        InitializeComponent();
    }
}
