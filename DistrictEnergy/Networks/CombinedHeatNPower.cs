using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks
{
    public class CombinedHeatNPower
    {
        [DataMember]
        [DefaultValue(TrakingModeEnum.Thermal)]
        public TrakingModeEnum TrackingMode { get; set; } = TrakingModeEnum.Thermal;

        [DataMember]
        [DefaultValue(0.5)]
        public double CapacityAsPeakLoad { get; set; } = 0.5;

        [DataMember]
        [DefaultValue(0.22)]
        public double ElectricalEfficiency { get; set; } = 0.22;

        [DataMember]
        [DefaultValue(0.29)]
        public double WasteHeatRecovery { get; set; } = 0.29;

    }

    [DataContract(Name = "TrackingMode")]
    public enum TrakingModeEnum
    {
        [EnumMember]
        Thermal,
        [EnumMember]
        Electrical,
    }
}