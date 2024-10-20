using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Point = System.Windows.Point;

namespace RecursionExplorer.Desktop;

public partial class FractalWindow : Window
{
    private Point _lastPanMousePosition;
    private bool _isDragging;
    
    private double _zoomFactor = 1.0; // Default zoom factor
    private double _panOffsetX = 0.0; // X offset for panning
    private double _panOffsetY = 0.0; // Y offset for panning

    private const double ZoomDelta = 0.1;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public FractalWindow()
    {
        InitializeComponent();
    }

    private void Draw(Color? color = null)
    {
        color ??= Color.Blue;
    
        double width = FractalImageContainer.ActualWidth;
        double height = FractalImageContainer.ActualHeight;

        var image = new Image<Rgba32>((int)width, (int)height);
    
        double radius = (100 * _zoomFactor);
    
        int positionX = (int)(width / 2 + _panOffsetX);
        int positionY = (int)(height / 2 + _panOffsetY);
    
        var shape = new EllipsePolygon(positionX, positionY, (int)radius);
        
        image.Mutate(ctx =>
        {
            ctx.Fill(Color.White);
            ctx.Fill(color.Value, shape);
        });

        var stream = new MemoryStream();
        image.SaveAsPng(stream);
    
        BitmapImage bitmap = new();
        
        bitmap.BeginInit();
        bitmap.StreamSource = stream;
        bitmap.EndInit();
    
        FractalImage.Source = bitmap;
    }
    
    private void DrawButton_OnClick(object sender, RoutedEventArgs e)
    {
        object selectedItem = FractalComboBox.SelectedItem;
        string fractalName = selectedItem is null ? "None" : ((ComboBoxItem)selectedItem).Name;

        MessageBox.Show("Selected fractal: " + fractalName);
    }

    private async void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        await _cancellationTokenSource.CancelAsync();
    }
    
    private void FractalImageContainer_OnLoaded(object? sender, EventArgs e)
    {
        Draw();
    }
    
    private void FractalImageContainer_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double zoomChange = e.Delta > 0 ? 1 + ZoomDelta : 1 - ZoomDelta;
        _zoomFactor *= zoomChange;

        Draw();
    }

    private void FractalImageContainer_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _lastPanMousePosition = e.GetPosition(this);
        FractalImageContainer.CaptureMouse();
    }

    private void FractalImageContainer_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        FractalImageContainer.ReleaseMouseCapture();

        Draw();
    }

    private void FractalImageContainer_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
    
        Point currentMousePosition = e.GetPosition(this);

        double mousePositionOffsetX = currentMousePosition.X - _lastPanMousePosition.X;
        double mousePositionOffsetY = currentMousePosition.Y - _lastPanMousePosition.Y;

        // if the offset is negative, we move in the opposite direction
        _panOffsetX += mousePositionOffsetX;
        _panOffsetY += mousePositionOffsetY;

        _lastPanMousePosition = currentMousePosition;

        Draw();
    }
}