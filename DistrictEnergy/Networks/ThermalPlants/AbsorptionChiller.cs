using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class AbsorptionChiller : IThermalPlantSettings
    {
        /// <summary>
        ///     Capacity as percent of peak cooling load (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_ABS { get; set; } = 0;

        /// <summary>
        ///     Cooling coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(0.90)]
        public double CCOP_ABS { get; set; } = 0.90;

        /// <summary>
        /// Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(633)]
        public double F { get; set; } = 633;

        /// <summary>
        /// Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0.0004)]
        public double V { get; set; } = 0.0004;

        public double Capacity { get; set; } = double.PositiveInfinity;

        [DataMember]
        [DefaultValue("Absorption Chiller")]
        public string Name { get; set; } = "Absorption Chiller";

        public Guid Id { get; set; } = Guid.NewGuid();

        public LoadTypes LoadType { get; set; } = LoadTypes.Cooling;
    }
}