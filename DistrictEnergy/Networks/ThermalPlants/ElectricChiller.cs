using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class ElectricChiller : IThermalPlantSettings
    {
        /// <summary>
        ///     Cooling coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(4.40)] public double CCOP_ECH { get; set; } = 4.40;
    }
}