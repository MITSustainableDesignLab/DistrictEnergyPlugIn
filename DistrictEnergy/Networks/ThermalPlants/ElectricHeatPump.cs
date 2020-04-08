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
        [DefaultValue(0)]
        public double OFF_EHP { get; set; } = 0;

        /// <summary>
        ///     Heating coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(3.2)]
        public double HCOP_EHP { get; set; } = 3.2;

        /// <summary>
        ///     Should the evaporator side be used as a cold source?
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public int UseEhpEvap { get; set; } = 0;

        [DataMember] [DefaultValue(1660)] public double F { get; set; } = 1660;
        [DataMember] [DefaultValue(0.00332)] public double V { get; set; } = 0.00332;
    }
}