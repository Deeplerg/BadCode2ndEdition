using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using RecursionExplorer.Desktop.Fractals;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Convolution;
using SixLabors.ImageSharp.Processing.Processors.Effects;
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
    private IFractalRenderer _renderer;
    
    public FractalWindow()
    {
        InitializeComponent();

        _renderer = new BurningShipFractalRenderer(RenderDevice.GPU);
    }
    
    private void Draw()
    {
        double width = FractalImageContainer.ActualWidth;
        double height = FractalImageContainer.ActualHeight;

        var imageWidth = (int)width;
        var imageHeight = (int)height;
        
        var image = _renderer.Render(imageWidth, imageHeight, _zoomFactor, _panOffsetX, _panOffsetY);

        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        
        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();

        FractalImage.Source = bitmapImage;
    }

    private static void MandelbrotKernel(Index1D index, ArrayView1D<int, Stride1D.Dense> data, int imgWidth, int imgHeight, double zoom, double panX, double panY)
    {
        int x = index % imgWidth;
        int y = index / imgWidth;

        // Adjust coordinates based on zoom and pan, using higher precision for zoom
        double real = (x - imgWidth / 2.0) * (3.0 / imgWidth) / zoom - panX - 0.5;
        double imaginary = (y - imgHeight / 2.0) * (3.0 / imgHeight) / zoom - panY;

        // Mandelbrot iteration
        double zx = 0.0;
        double zy = 0.0;
        int iteration = 0;

        // Scale the maximum iterations with the zoom level to avoid unnecessary work at deep zoom
        int maxIteration = 1000;

        while (zx * zx + zy * zy < 4.0 && iteration < maxIteration)
        {
            double temp = zx * zx - zy * zy + real;
            zy = 2.0 * zx * zy + imaginary;
            zx = temp;
            iteration++;
        }

        // Map iterations to color (grayscale)
        int colorValue = iteration == maxIteration ? 0 : (iteration % 256);
        data[index] = colorValue;
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

        // Calculate the mouse movement difference
        double mousePositionOffsetX = currentMousePosition.X - _lastPanMousePosition.X;
        double mousePositionOffsetY = currentMousePosition.Y - _lastPanMousePosition.Y;

        // Adjust the pan offsets, factoring in the current zoom level
        _panOffsetX += mousePositionOffsetX / _zoomFactor * 2;
        _panOffsetY += mousePositionOffsetY / _zoomFactor * 2;

        _lastPanMousePosition = currentMousePosition;

        Draw();
    }
}