using System.Collections.Generic;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class WindInput : NotStorage
    {
        public abstract List<double> WindAvailableInput(int t = 0, int dt = 8760);
        public abstract List<double> PowerPerTurbine(int t = 0, int dt = 8760);
    }
}