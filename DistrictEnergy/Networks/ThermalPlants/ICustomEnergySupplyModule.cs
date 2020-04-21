using System;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal interface ICustomEnergySupplyModule : IThermalPlantSettings
    {
        double F { get; set; }
        double V { get; set; }

        /// <summary>
        /// Path of the CSV File
        /// </summary>
        String Path { get; set; }

        /// <summary>
        /// Hourly Data Array
        /// </summary>
        double[] Data { get; set; }

        /// <summary>
        /// Unique identifier 
        /// </summary>
        Guid Id { get; set; }

        /// <summary>
        /// Name of the Custom Energy Supply Module
        /// </summary>
        string Name { get; set; }

        void LoadCsv();
        double ComputeHeatBalance(double demand, double chiller, double solar, int i);
    }
}