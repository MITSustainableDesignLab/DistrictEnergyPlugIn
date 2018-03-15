using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class NatGasBoiler : IThermalPlantSettings
    {
        /// <summary>
        ///     Heating efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.84)] public double EFF_NGB { get; set; } = 0.84; // (SLD) I thought 70% was quite low
    }
}