using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class BatteryBank : IThermalPlantSettings
    {
        public BatteryBank()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Elec, 1}
            };
        }
        /// <summary>
        ///     Capacity as number of days of autonomy (#)
        /// </summary>
        [DataMember]
        [DefaultValue(0)] public double AUT_BAT { get; set; } = 0;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)] public double LOSS_BAT { get; set; } = 0.15;

        /// <summary>
        ///     The Battery charged state at the beginning of the simulation. Assumed at 0%.
        /// </summary>
        [DataMember]
        [DefaultValue(0.80)] public double BAT_START { get; set; } = 0.80;

        /// <summary>
        /// Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(1383)] public double F { get; set; } = 1383;

        /// <summary>
        /// Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0)] public double V { get; set; } = 0;

        public double Capacity { get; set; } = double.PositiveInfinity;
        [DataMember]
        [DefaultValue("Battery")] public string Name { get; set; } = "Battery";
        public Guid Id { get; set; } = new Guid();
        public LoadTypes LoadType { get; set; } = LoadTypes.Elec;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Output { get; set; }
    }
}