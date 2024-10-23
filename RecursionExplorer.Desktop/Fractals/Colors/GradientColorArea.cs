using SixLabors.ImageSharp.PixelFormats;

namespace RecursionExplorer.Desktop.Fractals.Colors;

public record struct GradientColorArea(
    double PercentTaken,
    Rgba32 Color);