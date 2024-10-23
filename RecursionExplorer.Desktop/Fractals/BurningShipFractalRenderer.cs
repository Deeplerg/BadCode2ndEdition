using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using RecursionExplorer.Desktop.Fractals.Colors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RecursionExplorer.Desktop.Fractals;

public class BurningShipFractalRenderer : IFractalRenderer
{
    private readonly Accelerator _accelerator;
    private readonly Action<Index1D, ArrayView1D<byte, Stride1D.Dense>, int, int, double, double, double> _kernel;
    private const int MaxIterations = 1000;
    
    private Gradient _gradient;
    
    byte[]? _colorValues = null;
    private int _previousWidth;
    private int _previousHeight;
    
    public BurningShipFractalRenderer(RenderDevice device)
    {
        var context = Context.Create(builder => builder.Default().EnableAlgorithms());
        
        switch (device)
        {
            case RenderDevice.CPU:
                _accelerator = context.CreateCPUAccelerator(0);
                break;
            case RenderDevice.CUDA:
                GuardAgainstNoDevice<CudaDevice>(context, nameof(RenderDevice.CUDA));
                
                _accelerator = context.CreateCudaAccelerator(0);
                break;
            case RenderDevice.OpenCL:
                GuardAgainstNoDevice<CLDevice>(context, nameof(RenderDevice.OpenCL));

                _accelerator = context.CreateCLAccelerator(0);
                break;
            default:
                throw new InvalidEnumArgumentException(
                    nameof(device), (int)device, typeof(RenderDevice));
        }

        _kernel = _accelerator
            .LoadAutoGroupedStreamKernel<
                Index1D, ArrayView1D<byte, Stride1D.Dense>, int, int, double, double, double>(
                CalculateGrayscalePixel);

        // gradient = new Gradient(
        // [
        //     new GradientColorArea(0,         Color.Black.ToPixel<Rgba32>()),
        //     new GradientColorArea(1d / 16d,  Color.Red.ToPixel<Rgba32>()),
        //     new GradientColorArea(1d / 8d,   Color.Orange.ToPixel<Rgba32>()),
        //     new GradientColorArea(1d / 32d,  Color.Yellow.ToPixel<Rgba32>()),
        //     new GradientColorArea(0,         Color.White.ToPixel<Rgba32>())
        // ]);
        
        _gradient = new Gradient(new[]
        {
            new GradientColorZone(0,       Color.Black.ToPixel<Rgba32>()),
            new GradientColorZone(0.0625,  Color.Red.ToPixel<Rgba32>()),
            new GradientColorZone(0.09375, Color.Orange.ToPixel<Rgba32>()),
            new GradientColorZone(0.21875, Color.Yellow.ToPixel<Rgba32>()),
            new GradientColorZone(1,       Color.White.ToPixel<Rgba32>())
        });
    }
    
    public Image<Rgba32> Render(int width, int height, double zoom, double panX, double panY)
    {
        using var buffer = _accelerator.Allocate1D<byte>(width * height);

        if (_colorValues is null || _previousWidth != width || _previousHeight != height)
        {
            _colorValues = new byte[width * height];
            _previousWidth = width;
            _previousHeight = height;
        }
        var gridDimensions = new Index1D(width * height);

        double normalizedPanX = panX / width;
        double normalizedPanY = panY / height;
        
        _kernel.Invoke(gridDimensions, buffer.View, width, height, zoom, normalizedPanX, normalizedPanY);
        
        _accelerator.Synchronize();

        buffer.CopyToCPU(_colorValues);
        
        var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte colorValue = colorValues[y * width + x];
                var fractalColor = GetFractalColor(colorValue);
    
                image[x, y] = fractalColor;
            }
        }
    
        return image;
    }
    
    /// <summary>
    /// Calculates the number of fractal iterations for the current pixel and assigns
    /// a grayscale value to it based on the number of iterations.
    /// </summary>
    /// <param name="index">Current pixel</param>
    /// <param name="pixelColors">Shared array</param>
    /// <param name="width">Width of the image</param>
    /// <param name="height">Height of the image</param>
    /// <param name="zoom">Zoom level (higher is deeper)</param>
    /// <param name="panX">Total panning (x)</param>
    /// <param name="panY">Total panning (y)</param>
    /// <remarks>1 thread per pixel</remarks>
    private static void CalculateGrayscalePixel(
        Index1D index, 
        ArrayView1D<byte, Stride1D.Dense> pixelColors, 
        int width, int height, double zoom, double panX, double panY)
    {
        // X, Y of the current pixel
        int pixelX = index % width;
        int pixelY = index / width;
        
        double cReal = (pixelX - width / 2.0) * (3.0 / width) / zoom + panX - 1.76; // was: -0.5
        double cImaginary = (pixelY - height / 2.0) * (3.0 / height) / zoom + panY - 0.03;

        int iterations = CalculateIterations(0, 0, cReal, cImaginary);

        byte colorValue = iterations == MaxIterations ? (byte)0 : (byte)(iterations % 256);
        pixelColors[index] = colorValue;
    }

    private static int CalculateIterations(double x, double y, double cReal, double cImaginary)
    {
        int iteration = 0;

        double zReal = x;
        double zImaginary = y;

        while (!HasEscaped(zReal, zImaginary, iteration))
        {
            double absoluteReal = Math.Abs(zReal);
            double absoluteImaginary = Math.Abs(zImaginary);
            
            //(a + bi)^2 = (a^2 − b^2) + 2abi
            
            double newReal = (absoluteReal * absoluteReal - absoluteImaginary * absoluteImaginary) + cReal;
            zImaginary = 2 * absoluteReal * absoluteImaginary + cImaginary;
            zReal = newReal;

            iteration++;
        }

        return iteration;
    }

    private static bool HasEscaped(double zReal, double zImaginary, int iteration)
    {
        // Magnitude of the complex number z: |z| = sqrt(zx^2 + zy^2).
        // sqrt is an expensive operation, so instead we're doing this:
        // |z|^2 = zx^2 + zy^2
        double squaredMagnitude = zReal * zReal + zImaginary * zImaginary;

        // The magnitude at which the point is considered to have escaped to infinity.
        // For this fractal, it's 2, but because we're squaring,
        // it's 2^2=4.
        double escapedMagnitude = 4;

        bool hasMagnitudeEscaped = squaredMagnitude >= escapedMagnitude;
        bool hasIterationEscaped = iteration >= MaxIterations;
        return hasMagnitudeEscaped || hasIterationEscaped;
    }


    private Rgba32 GetFractalColor(byte iterationValue)
        => GetFractalColorGradient(iterationValue);
    
    private Rgba32 GetFractalColorStep(byte iterationValue)
    {
        return iterationValue switch
        {
            0 => new Rgba32(0, 0, 0, 255),
            < 32 => new Rgba32(64, 0, 0, 255),
            < 64 => new Rgba32(128, 0, 0, 255),
            < 128 => new Rgba32(255, 69, 0, 255),
            < 192 => new Rgba32(255, 140, 0, 255),
            _ => new Rgba32(255, 165, 0, 255)
        };
    }
    
    Rgba32 GetFractalColorLinear(byte iterationValue)
    {
        if (iterationValue == 0)
            return new Rgba32(0, 0, 0, 255);
    
        byte r = (byte)(64 + (iterationValue * 191.0 / 255.0));  // 64 to 255
        byte g = (byte)(iterationValue * 40.0 / 255.0);         // 0 to 40
        return new Rgba32(r, g, 0, 255);
    }
    
    private Rgba32 GetFractalColorGradient(byte iterationValue)
    {
        if (iterationValue == 0)
            return Color.Black.ToPixel<Rgba32>();

        // Normalize to [0,1]
        double normalizedIteration = iterationValue / (double)MaxIterations;

        return _gradient.CalculateColor(normalizedIteration);
    }

    private void GuardAgainstNoDevice<TDevice>(Context context, string? deviceName = null)
        where TDevice : Device
    {
        var clDevices = context.GetDevices<TDevice>();
        if (clDevices.Count == 0)
            throw new NotSupportedException(
                $"No {deviceName ?? typeof(TDevice).Name} {(deviceName is null ? string.Empty : "device ")}found.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _accelerator.Dispose();
        }
    }

    ~BurningShipFractalRenderer()
    {
        Dispose(false);
    }
}