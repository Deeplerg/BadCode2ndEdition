using SixLabors.ImageSharp.PixelFormats;

namespace RecursionExplorer.Desktop.Fractals.Colors;

public record struct GradientColorZone(
    double Position,
    Rgba32 Color);