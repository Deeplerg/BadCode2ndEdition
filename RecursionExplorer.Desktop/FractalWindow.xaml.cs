using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using RecursionExplorer.Desktop.Fractals;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Point = System.Windows.Point;
using ResizeMode = System.Windows.ResizeMode;

namespace RecursionExplorer.Desktop;

public partial class FractalWindow : Window
{
    private Point _lastPanMousePosition;
    private bool _isDragging;

    private double _zoomFactor = 1.0; // Default zoom factor
    private double _panOffsetX = 0.0; // X offset for panning
    private double _panOffsetY = 0.0; // Y offset for panning

    private const double ZoomDelta = 0.1;
    private IFractalRenderer _renderer;
    private RenderDevice _currentRenderDevice;

    private const Key FullscreenModeKey = Key.F11; 
    private bool _isMaximized = false;

    private readonly Stopwatch _stopwatch = new Stopwatch();
    
    public FractalWindow()
    {
        InitializeComponent();

        _renderer = null!; // clear the uninitialized warning
        CreateNewRenderer(RenderDevice.CPU);
    }
    
    private void Draw()
    {
        DeviceComboBox.IsEditable = false;
        
        _stopwatch.Restart();

        var image = RenderImage();
        SaveImage(image);

        _stopwatch.Stop();
        
        double totalMilliseconds = _stopwatch.ElapsedMilliseconds;
        int fps = (int)(1000 / totalMilliseconds);
        LastDrawTookTimeLabel.Content = $"{totalMilliseconds}мс ({fps}FPS)";
        
        LastDrawTookInfoLabel.Visibility = Visibility.Visible;
        LastDrawTookTimeLabel.Visibility = Visibility.Visible;
    }

    private Image<Rgba32> RenderImage()
    {
        double width = FractalImageContainer.ActualWidth;
        double height = FractalImageContainer.ActualHeight;

        var imageWidth = (int)width;
        var imageHeight = (int)height;
        
        return _renderer.Render(imageWidth, imageHeight, _zoomFactor, _panOffsetX, _panOffsetY);
    }

    private void SaveImage(Image<Rgba32> image)
    {
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        
        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();

        FractalImage.Source = bitmapImage;
    }
    
    private RenderDevice GetRenderDeviceFromComboBox(string name)
    {
        return name switch
        {
            "CpuDeviceComboBoxItem" => RenderDevice.CPU,
            "CudaDeviceComboBoxItem" => RenderDevice.CUDA,
            "OpenClDeviceComboBoxItem" => RenderDevice.OpenCL,
            _ => throw new ArgumentException($"No known {nameof(RenderDevice)} for {name}.")
        };
    }
    
    private void CreateNewRenderer(RenderDevice device)
    {
        _renderer = new BurningShipFractalRenderer(device);
        _currentRenderDevice = device;
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
        _panOffsetX -= mousePositionOffsetX / _zoomFactor * 2;
        _panOffsetY -= mousePositionOffsetY / _zoomFactor * 2;

        _lastPanMousePosition = currentMousePosition;

        Draw();
    }

    private void DeviceComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string? name = (DeviceComboBox.SelectedItem as ComboBoxItem)?.Name;
        if (string.IsNullOrEmpty(name))
            return;

        
        var device = GetRenderDeviceFromComboBox(name);
        if (device == _currentRenderDevice)
            return;

        var currentRenderer = _renderer;
        try
        {
            CreateNewRenderer(device);
            currentRenderer.Dispose();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error! " + ex.Message);
        }
    }

    private void FractalWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        KeyDown += OnKeyDown;
    }

    private void RedrawButton_OnClick(object sender, RoutedEventArgs e)
    {
        Draw();
    }

    private void AspectRatioButton_OnClick(object sender, RoutedEventArgs e)
    {
        double imageWidth = FractalImageContainer.ActualWidth;
        double imageHeight = FractalImageContainer.ActualHeight;
        
        double imageDimensionDifference = Math.Abs(imageWidth - imageHeight);

        if (imageWidth > imageHeight)
        {
            this.Height += imageDimensionDifference;
        }
        else
        {
            this.Width += imageDimensionDifference;
        }

        Draw();
    }
    
    private void FractalWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Draw();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != FullscreenModeKey)
            return;

        if (!_isMaximized)
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
            ControlDockPanel.Visibility = Visibility.Collapsed;
            
            _isMaximized = true;
        }
        else
        {
            Topmost = false;
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            ResizeMode = ResizeMode.CanResize;
            ControlDockPanel.Visibility = Visibility.Visible;

            _isMaximized = false;
        }

        Draw();
    }
}