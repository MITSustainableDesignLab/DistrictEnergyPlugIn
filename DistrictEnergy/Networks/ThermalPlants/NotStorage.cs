﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class NotStorage : IThermalPlantSettings
    {
        public abstract double Efficiency { get; }
        public abstract LoadTypes InputType { get; }
        public abstract double F { get; set; }
        public abstract double V { get; set; }
        public abstract double Capacity { get; set; }
        public abstract string Name { get; set; }
        public abstract Guid Id { get; set; }
        public abstract LoadTypes OutputType { get; }
        public abstract Dictionary<LoadTypes, double> ConversionMatrix { get; }
        public abstract List<DateTimePoint> Input { get; set; }
        public abstract List<DateTimePoint> Output { get; set; }
        public abstract Dictionary<LoadTypes, SolidColorBrush> Fill { get; set; }
        public GraphCost FixedCost => new FixedCost(this);
        public GraphCost VariableCost => new VariableCost(this, 200);
        public double TotalCost => FixedCost.Cost + VariableCost.Cost;
        public abstract double CapacityFactor { get; set; }
        public abstract bool IsForced { get; set; }
    }
}