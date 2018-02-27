using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class SolarThermalCollector
    {
        [DataMember]
        [DefaultValue(0.1)]
        public double TargetOffset { get; set; } = 0.1;
        [DataMember]
        [DefaultValue(0.45)]
        public double CollectorEfficiency { get; set; } = 0.45;
        [DataMember]
        [DefaultValue(0.75)]
        public double AreaUtilFactor { get; set; } = 0.75;
        [DataMember]
        [DefaultValue(0.15)]
        public double MiscLosses { get; set; } = 0.15;
    }
}