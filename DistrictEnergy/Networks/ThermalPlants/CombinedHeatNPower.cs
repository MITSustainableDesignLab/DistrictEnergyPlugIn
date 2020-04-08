﻿using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class CombinedHeatNPower : IThermalPlantSettings
    {
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
        [DefaultValue(0)] public double OFF_CHP { get; set; } = 0;

        /// <summary>
        ///     Electrical Efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.22)] public double EFF_CHP { get; set; } = 0.22;

        /// <summary>
        ///     Waste heat recovery effectiveness (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.29)] public double HREC_CHP { get; set; } = 0.29;

        /// <summary>
        /// Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(1606)] public double F { get; set; } = 1606;

        /// <summary>
        /// Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0.010)] public double V { get; set; } = 0.010;
    }

    [DataContract(Name = "TMOD_CHP")]
    public enum TrakingModeEnum
    {
        [EnumMember] Thermal,
        [EnumMember] Electrical
    }
}