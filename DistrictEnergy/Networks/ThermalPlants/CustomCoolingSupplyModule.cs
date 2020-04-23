using System;
using System.Windows.Media;
using Rhino.Render;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomCoolingSupplyModule : CustomEnergySupplyModule
    {

        public CustomCoolingSupplyModule()
        {
            Instance = this;
        }

        public CustomCoolingSupplyModule Instance { get; set; }

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
    }
}