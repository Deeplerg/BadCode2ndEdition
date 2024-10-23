using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RecursionExplorer.Desktop.Fractals;

public interface IFractalRenderer : IDisposable
{
    Image<Rgba32> Render(int width, int height, double zoom, double panX, double panY);
}