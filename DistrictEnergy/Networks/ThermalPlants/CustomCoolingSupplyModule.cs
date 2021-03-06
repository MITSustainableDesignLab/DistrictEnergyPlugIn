﻿using System;
using System.Collections.Generic;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomCoolingSupplyModule : CustomEnergySupplyModule
    {
        public double[] Used = new double[8760];

        public override LoadTypes OutputType => LoadTypes.Cooling;
        public override double OFF_Custom { get; set; } = 1;
        public override LoadTypes InputType => LoadTypes.Custom;

        public override double CapacityFactor
        {
            get => OFF_Custom;
            set { }
        }

        public override bool IsForced { get; set; }

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {OutputType, 1},
            {InputType, -1}
        };

        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double F { get; set; }
        public override double V { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];

        public override double Capacity
        {
            get => Output.Max();
            set => throw new NotImplementedException();
        }

        public override double CarbonIntensity { get; set; }

        public double ComputeHeatBalance(double demand, int i)
        {
            var custom = Data[i] * Math.Abs(1 / Norm);
            var excess = Math.Max(custom - demand, 0);
            var used = Math.Min(demand, custom);
            var balance = demand - (custom - excess);
            Used[i] = used;
            return used;
        }
    }
}