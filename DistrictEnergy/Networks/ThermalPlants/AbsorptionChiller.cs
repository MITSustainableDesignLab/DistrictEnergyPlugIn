using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class AbsorptionChiller : IThermalPlantSettings
    {
        /// <summary>
        ///     Capacity as percent of peak cooling load (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)] public double OFF_ABS { get; set; } = 0;

        /// <summary>
        ///     Cooling coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(0.90)] public double CCOP_ABS { get; set; } = 0.90;
    }
}