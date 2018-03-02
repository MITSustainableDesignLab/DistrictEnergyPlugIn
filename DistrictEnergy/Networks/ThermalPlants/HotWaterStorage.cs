using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class HotWaterStorage
    {
        /// <summary>
        /// Capacity as number of days of autonomy (#)
        /// </summary>
        [DataMember]
        [DefaultValue(1.0)]
        public double AUT_HWT { get; set; } = 1.0;

        /// <summary>
        /// Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double LOSS_HWT { get; set; } = 0.15;
    }
}