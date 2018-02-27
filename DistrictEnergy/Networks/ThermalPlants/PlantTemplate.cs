using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class PlantTemplate : LibraryComponent
    {
        [DataContract]
        public class BuildingTemplate
        {
            [DataMember]
            public ElectricHeatPump ElectricHeatPump { get; set; }
            [DataMember]
            public NatGasBoiler NatGasBoiler { get; set; }
            [DataMember]
            public SolarThermalCollector SoalrThermalCollector { get; set; }
            [DataMember]
            public HotWaterStorage HotWaterStorage { get; set; }
            [DataMember]
            public CombinedHeatNPower CombinedHeatNPower { get; set; }
        }

    }
}