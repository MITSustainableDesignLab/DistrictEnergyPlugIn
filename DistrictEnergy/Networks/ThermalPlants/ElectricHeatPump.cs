using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class ElectricHeatPump
    {

        [DataMember]
        [DefaultValue(0)]
        public double CapacityAsPeakLoad { get; set; } = 0;

        [DataMember]
        [DefaultValue(3.2)]
        public double HeatingCOP { get; set; } = 3.2;

    }
}