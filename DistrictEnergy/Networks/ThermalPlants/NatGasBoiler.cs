using System;
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

        [DataMember] [DefaultValue(1360)] public double F { get; set; } = 1360;
        [DataMember] [DefaultValue(0)] public double V { get; set; }
        public double Capacity { get; set; } = double.PositiveInfinity;
        public string Name { get; set; } = "Natural Gas Boiler";
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}