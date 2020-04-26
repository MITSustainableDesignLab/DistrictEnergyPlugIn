using System;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomHeatingSupplyModule : CustomEnergySupplyModule
    {
        public new double F
        {
            set { F = value; }
            get { return F; }
        }

        public new double V
        {
            set { V = value; }
            get { return V; }
        }

        public CustomHeatingSupplyModule()
        {
        }

        public double ComputeHeatBalance(double demand, double chiller, double solar, int i)
        {
            var custom = Data[i];
            var excess = Math.Max((chiller + solar + custom) - demand, 0);
            var balance = demand - (chiller + solar + custom - excess);
            return balance;
        }

        public LoadTypes LoadType { get; set; } = LoadTypes.Heating;
    }
}