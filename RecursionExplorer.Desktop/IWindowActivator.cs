using System.Windows;

namespace RecursionExplorer.Desktop;

public interface IWindowActivator
{
    T CreateInstance<T>() where T : Window;
}