using System;
using Rhino.Geometry;

namespace DistrictEnergy.Networks
{
    public interface IThermalPlant
    {
        int? StartIndex { get; }
        bool IsVisible { get; }
        GeometryBase Geometry { get; }
        Guid Id { get; }
        string Name { get; }
        PlantTemplate Template { get; }
    }
}