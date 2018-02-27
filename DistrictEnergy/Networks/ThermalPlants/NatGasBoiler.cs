using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class NatGasBoiler
    {
        [DataMember]
        [DefaultValue(0.7)]
        public double HeatEfficiency { get; set; } = 0.7;
    }
}