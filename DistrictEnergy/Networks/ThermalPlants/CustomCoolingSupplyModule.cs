using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using Rhino.Render;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomCoolingSupplyModule : CustomEnergySupplyModule
    {
        public CustomCoolingSupplyModule()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Cooling, 1}
            };
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
        public override LoadTypes LoadType { get; set; } = LoadTypes.Cooling;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public override double[] Output { get; set; }
        public override double F { get; set; }
        public override double V { get; set; }
        public override double Efficiency => ConversionMatrix[LoadType];
        public override double Capacity
        {
            get => Data.Max();
            set => throw new NotImplementedException();
        }
    }
}