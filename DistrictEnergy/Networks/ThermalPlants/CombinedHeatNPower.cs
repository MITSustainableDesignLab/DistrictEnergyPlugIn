using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class CombinedHeatNPower : IDispatchable
    {
        public CombinedHeatNPower()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Elec, EFF_CHP},
                {LoadTypes.Heating, HREC_CHP},
                {LoadTypes.Gas, -1},
            };
        }

        /// <summary>
        ///     Tracking Mode
        /// </summary>
        [DataMember]
        [DefaultValue(TrakingModeEnum.Thermal)]
        public TrakingModeEnum TMOD_CHP { get; set; } = TrakingModeEnum.Thermal;

        /// <summary>
        ///     Capacity as percent of peak electric load (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_CHP { get; set; } = 0;

        /// <summary>
        ///     Electrical Efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.22)]
        public double EFF_CHP { get; set; } = 0.22;

        /// <summary>
        ///     Waste heat recovery effectiveness (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.29)]
        public double HREC_CHP { get; set; } = 0.29;

        /// <summary>
        /// Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(1606)]
        public double F { get; set; } = 1606;

        /// <summary>
        /// Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0.010)]
        public double V { get; set; } = 0.010;

        public double Capacity { get; set; } = 0;

        [DataMember]
        [DefaultValue("Combined Heat&Power")]
        public string Name { get; set; } = "Combined Heat&Power";

        public Guid Id { get; set; } = Guid.NewGuid();
        public LoadTypes LoadType { get; set; } = LoadTypes.Elec;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Output { get; set; }
        public double Efficiency => ConversionMatrix[LoadType];
    }

    [DataContract(Name = "TMOD_CHP")]
    public enum TrakingModeEnum
    {
        [EnumMember] Thermal,
        [EnumMember] Electrical
    }
}