using SixLabors.ImageSharp.PixelFormats;

namespace RecursionExplorer.Desktop.Fractals.Colors;

public class Gradient
{
    private readonly List<GradientColorZone> _zones;

    public Gradient(IEnumerable<GradientColorZone> zones)
    {
        _zones = zones
            .OrderBy(x => x.Position)
            .ToList();
        
        if (!_zones.Any())
            throw new ArgumentException("No zones were provided.", nameof(zones));
        
        EnsureFirstAndLastZones();
    }

    public Gradient(IEnumerable<GradientColorArea> areas)
    {
        var enumeratedAreas = areas.ToList();

        if (enumeratedAreas.Count < 2)
            throw new ArgumentException(
                "There must be at least two areas. Got: " + enumeratedAreas.Count, nameof(areas));

        double totalPercentTaken = enumeratedAreas.Sum(x => x.PercentTaken);
        if(totalPercentTaken > 1)
            throw new ArgumentException(
                $"Total {nameof(GradientColorArea.PercentTaken)} cannot exceed 1. " +
                $"Got: " + totalPercentTaken, nameof(areas));
        
        _zones = new List<GradientColorZone>(enumeratedAreas.Count);
        double previous = 0;
        foreach(var area in enumeratedAreas)
        {
            double totalPercent = area.PercentTaken + previous;
            _zones.Add(new GradientColorZone(totalPercent, area.Color));
            previous = totalPercent;
        }
        
        EnsureFirstAndLastZones();
    }
    
    public Rgba32 CalculateColor(double position)
    {
        if (position < 0 || position > 1)
            throw new ArgumentException("Position must be between 0 and 1.", nameof(position));

        int nextZoneIndex = FindIndexWithHigherPositionThan(position);
        if (nextZoneIndex == -1)
            nextZoneIndex = _zones.Count - 1;

        var previousZone = _zones[nextZoneIndex - 1];
        var nextZone = _zones[nextZoneIndex];

        double previousPosition = previousZone.Position;
        double nextPosition = nextZone.Position;

        double percent = (position - previousPosition) / (nextPosition - previousPosition);

        return InterpolateColor(previousZone.Color, nextZone.Color, percent);
    }

    private Rgba32 InterpolateColor(Rgba32 previousColor, Rgba32 nextColor, double percent)
    {
        return new Rgba32(
            InterpolateColor(previousColor.R, nextColor.R, percent),
            InterpolateColor(previousColor.G, nextColor.G, percent),
            InterpolateColor(previousColor.B, nextColor.B, percent),
            byte.MaxValue);
    }
    
    private byte InterpolateColor(byte previousColor, byte nextColor, double percent)
    {
        return (byte)(previousColor + (nextColor - previousColor) * percent);
    }

    /// <summary>
    /// Allocation-free index search
    /// </summary>
    /// <returns>Index, or -1 if not found</returns>
    private int FindIndexWithHigherPositionThan(double position)
    {
        for (int i = 0; i < _zones.Count; i++)
        {
            var zone = _zones[i];
            if (zone.Position > position)
                return i;
        }

        return -1;
    }

    private void EnsureFirstAndLastZones()
    {
        if (_zones.First().Position > 0)
        {
            var firstColor = _zones.First().Color;
            _zones.Insert(0, new GradientColorZone(0, firstColor));
        }

        if (_zones.Last().Position < 1)
        {
            var lastColor = _zones.Last().Color;
            _zones.Add(new GradientColorZone(1, lastColor));
        }
    }
}