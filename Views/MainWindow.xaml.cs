using PFSoftware.Finances.Models;
using System.Windows;

namespace PFSoftware.Finances.Views
{
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        private void Window_Loaded(object sender, RoutedEventArgs e) => AppState.MainWindow = this;
    }
}