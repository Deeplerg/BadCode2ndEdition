using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
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

        // Initialize ILGPU context and accelerator
        using var context = Context.CreateDefault();
        using var accelerator = context.CreateCPUAccelerator(0); // Using CPU accelerator

        double width = FractalImageContainer.ActualWidth;
        double height = FractalImageContainer.ActualHeight;

        var imageWidth = (int)width;
        var imageHeight = (int)height;

        // Memory for the image on the CPU side
        var imageData = new int[imageWidth * imageHeight];

        // Allocate memory on the GPU side
        using var buffer = accelerator.Allocate1D<int>(imageWidth * imageHeight);

        // Kernel to compute Mandelbrot fractal with zoom and pan
        void MandelbrotKernel(Index1D index, ArrayView1D<int, Stride1D.Dense> data, int imgWidth, int imgHeight,
            double zoom, double panX, double panY)
        {
            int x = index % imgWidth;
            int y = index / imgWidth;

            // Adjust coordinates based on zoom and pan
            double real = (x - imgWidth / 2.0) * (3.0 / imgWidth) / zoom + panX - 0.5;
            double imaginary = (y - imgHeight / 2.0) * (3.0 / imgHeight) / zoom + panY;

            // Mandelbrot iteration
            double zx = 0.0;
            double zy = 0.0;
            int iteration = 0;
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

        // Launch the kernel with a 1D grid of threads (one thread per pixel)
        var gridDim = new Index1D(imageWidth * imageHeight);

        // Pass the zoom and pan offsets to the kernel
        var kernel = accelerator
            .LoadAutoGroupedStreamKernel<Index1D, ArrayView1D<int, Stride1D.Dense>, int, int, double, double, double>(
                MandelbrotKernel);
        kernel(gridDim, buffer.View, imageWidth, imageHeight, _zoomFactor, _panOffsetX / width, _panOffsetY / height);

        // Copy the result back to the CPU side
        buffer.CopyToCPU(imageData);

        // Create a ImageSharp image from the resulting image data
        var image = new Image<Rgba32>(imageWidth, imageHeight);
        for (int y = 0; y < imageHeight; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                int colorValue = imageData[y * imageWidth + x];
                var fractalColor = new Rgba32(255, (byte)colorValue, (byte)colorValue, (byte)colorValue); // Grayscale

                image[x, y] = fractalColor;
            }
        }

        // Save the image to a MemoryStream in PNG format and display it
        var stream = new MemoryStream();
        image.SaveAsPng(stream);

        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();

        FractalImage.Source = bitmapImage;
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