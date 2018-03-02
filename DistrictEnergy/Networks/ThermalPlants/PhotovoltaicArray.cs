﻿using System.ComponentModel;
using System.Runtime.Serialization;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class PhotovoltaicArray
    {
        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.05)] public double OFF_PV { get; set; } = 0.05;

        /// <summary>
        ///     Cell efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)] public double EFF_PV { get; set; } = 0.15;

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.75)] public double UTIL_PV { get; set; } = 0.75;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)] public double LOSS_PV { get; set; } = 0.15;
    }
}