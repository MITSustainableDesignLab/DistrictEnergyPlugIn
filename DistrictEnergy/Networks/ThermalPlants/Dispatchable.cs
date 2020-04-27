using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class Dispatchable : IThermalPlantSettings
    {
        public abstract LoadTypes InputType { get; set; }
        public abstract double F { get; set; }
        public abstract double V { get; set; }
        public abstract double Capacity { get; set; }
        public abstract string Name { get; set; }
        public abstract Guid Id { get; set; }
        public abstract LoadTypes OutputType { get; set; }
        public abstract Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public abstract List<DateTimePoint> Input { get; set; }
        public abstract List<DateTimePoint> Output { get; set; }
        public abstract double Efficiency { get; }
        public abstract SolidColorBrush Fill { get; set; }
    }
}