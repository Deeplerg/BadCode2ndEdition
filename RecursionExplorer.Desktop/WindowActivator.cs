using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace RecursionExplorer.Desktop;

public class WindowActivator(IServiceProvider serviceProvider) : IWindowActivator
{
    public T CreateInstance<T>() where T : Window
    {
        var form = ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider);
        return form;
    }
}