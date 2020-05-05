using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class Storage : IThermalPlantSettings
    {
        public abstract double ChargingEfficiency { get; }
        public abstract double DischargingEfficiency { get; }
        public double StorageStandingLosses { get; } = 0.001;
        [JsonIgnore] public List<DateTimePoint> Stored { get; set; }
        public abstract double MaxChargingRate { get; }
        public abstract double MaxDischargingRate { get; }
        public abstract double StartingCapacity { get; }
        public abstract double F { get; set; }
        public abstract double V { get; set; }
        public abstract double Capacity { get; }
        public abstract string Name { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public abstract LoadTypes OutputType { get; }
        public abstract Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public List<DateTimePoint> Input { get; set; }
        public List<DateTimePoint> Output { get; set; }
        public abstract Dictionary<LoadTypes, SolidColorBrush> Fill { get; set; }
        public abstract LoadTypes InputType { get; }
        public GraphCost FixedCost => new FixedCost(this);
        public GraphCost VariableCost => new VariableCost(this, 200);
        public double TotalCost => FixedCost.Cost + VariableCost.Cost;
        public double CapacityFactor
        {
            get => 1;
            set => throw new NotImplementedException();
        }

        public bool IsForced { get; set; }
    }
}