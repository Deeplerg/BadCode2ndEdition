using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace RecursionExplorer.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        //base.OnStartup(e);

        var startup = new Startup();

        var config = startup.ConfigureConfiguration();
        var services = startup.ConfigureServices(config);
        var provider = startup.BuildServiceProvider(services);

        var windowActivator = provider.GetRequiredService<IWindowActivator>();
        
        var mainWindow = windowActivator.CreateInstance<MainWindow>();

        mainWindow.Show();
    }
}