using System.Windows;
using System.Windows.Controls;

namespace RecursionExplorer.Desktop;

public partial class FractalWindow : Window
{
    public FractalWindow()
    {
        InitializeComponent();
    }

    private void DrawButton_OnClick(object sender, RoutedEventArgs e)
    {
        object selectedItem = FractalComboBox.SelectedItem;
        string fractalName = selectedItem is null ? "None" : ((ComboBoxItem)selectedItem).Name;

        MessageBox.Show("Selected fractal: " + fractalName);
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}