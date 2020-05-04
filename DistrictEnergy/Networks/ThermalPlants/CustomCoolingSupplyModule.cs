using System;
using System.Collections.Generic;
using System.Linq;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomCoolingSupplyModule : CustomEnergySupplyModule
    {
        public CustomCoolingSupplyModule()
        {
        }

        public double ComputeHeatBalance(double demand, int i)
        {
            var custom = Data[i] * Math.Abs(1 / Norm);
            var excess = Math.Max(custom - demand, 0);
            var used = Math.Min(demand, custom);
            var balance = demand - (custom - excess);
            Used[i] = used;
            return used;
        }

        public double[] Used = new double[8760];
        public override LoadTypes OutputType => LoadTypes.Cooling;
        public override double OFF_Custom { get; set; } = 1;
        public override LoadTypes InputType => LoadTypes.Custom;
        public override double CapacityFactor => OFF_Custom;
        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>()
        {
            {LoadTypes.Cooling, 1}
        };
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double F { get; set; }
        public override double V { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];

        public override double Capacity => Output.Max();
    }
}