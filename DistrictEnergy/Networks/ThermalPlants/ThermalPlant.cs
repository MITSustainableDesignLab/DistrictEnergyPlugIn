using System;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class ThermalPlant : IThermalPlant
    {
        public ThermalPlant(RhinoObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            Geometry = obj.Geometry;
            Id = obj.Id;
            IsVisible = obj.Attributes.Visible;
            Name = obj.Name;
        }

        public ThermalPlant(RhinoObject obj, string templateName) : this(obj)
        {
            TemplateName = templateName;
        }

        public ThermalPlant(RhinoObject obj, PlantTemplate template) : this(obj)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            Template = template;
            TemplateName = template.Name;
        }

        public string TemplateName { get; }

        public int? StartIndex { get; }

        public bool IsVisible { get; }

        public GeometryBase Geometry { get; }

        public Guid Id { get; }

        public string Name { get; }
        public PlantTemplate Template { get; }
    }
}