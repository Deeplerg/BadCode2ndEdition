using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RecursionExplorer.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IWindowActivator _activator;

    public MainWindow(IWindowActivator activator)
    {
        _activator = activator;
        
        InitializeComponent();
    }

    private void FractalButton_OnClick(object sender, RoutedEventArgs e)
    {
        CreateWindowAndShow<FractalWindow>();
    }

    private void CreateWindowAndShow<TWindow>() where TWindow : Window
    {
        var window = _activator.CreateInstance<TWindow>();
        window.Show();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        CreateWindowAndShow<HanoiTowersWindow>();
    }
}