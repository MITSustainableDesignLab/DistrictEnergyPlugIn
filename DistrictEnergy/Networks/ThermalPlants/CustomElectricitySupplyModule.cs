using System;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomElectricitySupplyModule : CustomEnergySupplyModule
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

        public CustomElectricitySupplyModule()
        {
            Instance = this;
        }

        public CustomElectricitySupplyModule Instance { get; set; }

        public double ComputeHeatBalance(double demand, double chiller, double solar, int i)
        {
            var custom = Data[i];
            var excess = Math.Max((chiller + solar + custom) - demand, 0);
            var balance = demand - (chiller + solar + custom - excess);
            return balance;
        }
    }
}