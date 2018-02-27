using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class HotWaterStorage
    {
        [DataMember]
        [DefaultValue(1.0)]
        public double CapacityAsAutonomusDays { get; set; } = 1.0;
        [DataMember]
        [DefaultValue(0.15)]
        public double MiscLosses { get; set; } = 0.15;
    }
}