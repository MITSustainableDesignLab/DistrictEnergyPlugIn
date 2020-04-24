using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class PhotovoltaicArray : IThermalPlantSettings
    {
        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_PV { get; set; } = 0;

        /// <summary>
        ///     Cell efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double EFF_PV { get; set; } = 0.15;

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.75)]
        public double UTIL_PV { get; set; } = 0.75;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double LOSS_PV { get; set; } = 0.15;

        [DataMember] [DefaultValue(1313)] public double F { get; set; } = 1313;
        [DataMember] [DefaultValue(0)] public double V { get; set; }
        public double Capacity { get; set; } = double.PositiveInfinity;
        [DataMember] [DefaultValue("PV")] public string Name { get; set; } = "PV";
        public Guid Id { get; set; } = Guid.NewGuid();
        public LoadTypes LoadType { get; set; } = LoadTypes.Elec;
    }
}