using System;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class CustomEnergySupplyModule : IThermalPlantSettings
    {
        public double F { get; set; }
        public double V { get; set; }

        /// <summary>
        /// Path of the CSV File
        /// </summary>
        public String Path { get; set; }
    }
}