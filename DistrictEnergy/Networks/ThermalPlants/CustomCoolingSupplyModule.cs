using System;
using Rhino.Render;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomCoolingSupplyModule : CustomEnergySupplyModule
    {
        public new double F
        {
            set { Instance.F = value; }
            get { return Instance.F; }
        }

        public new double V
        {
            set { Instance.V = value; }
            get { return Instance.V; }
        }

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
            return used;
        }
    }
}