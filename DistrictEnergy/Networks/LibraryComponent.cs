using System.Runtime.Serialization;

namespace DistrictEnergy.Networks
{
    public class LibraryComponent
    {
        [DataMember]
        public string Name { get; set; }
    }
}