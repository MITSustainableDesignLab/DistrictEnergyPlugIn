using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class ElectricChiller : IThermalPlantSettings
    {
        /// <summary>
        ///     Cooling coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(4.40)]
        public double CCOP_ECH { get; set; } = 4.40;

        [DataMember] [DefaultValue(164.1)] public double F { get; set; } = 164.1;
        [DataMember] [DefaultValue(0)] public double V { get; set; }
        public double Capacity { get; set; } = double.PositiveInfinity;
        [DataMember] [DefaultValue("Chiller")] public string Name { get; set; } = "Chiller";
        public Guid Id { get; set; }
        public LoadTypes LoadType { get; set; } = LoadTypes.Cooling;
    }
}