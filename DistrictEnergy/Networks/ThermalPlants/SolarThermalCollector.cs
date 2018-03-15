using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class SolarThermalCollector : IThermalPlantSettings
    {
        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)] public double OFF_SHW { get; set; } = 0;

        /// <summary>
        ///     Collector efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.45)] public double EFF_SHW { get; set; } = 0.45;

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.75)] public double UTIL_SHW { get; set; } = 0.75;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)] public double LOSS_SHW { get; set; } = 0.15;
    }
}