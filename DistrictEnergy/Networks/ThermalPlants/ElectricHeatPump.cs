using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class ElectricHeatPump : IThermalPlantSettings
    {
        /// <summary>
        ///     Capacity as percent of peak heating load (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)] public double OFF_EHP { get; set; } = 0;

        /// <summary>
        ///     Heating coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(3.2)] public double HCOP_EHP { get; set; } = 3.2;
    }
}