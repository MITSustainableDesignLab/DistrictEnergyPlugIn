using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class LibraryComponent
    {
        [DataMember]
        public string Name { get; set; }
    }
}