using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class BatteryBank : IThermalPlantSettings
    {
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
        ///     The Battery charged state at the begining of the simulation. Assumed at 80%.
        /// </summary>
        [DataMember]
        [DefaultValue(0.80)] public double BAT_START { get; set; } = 0.8;
    }
}