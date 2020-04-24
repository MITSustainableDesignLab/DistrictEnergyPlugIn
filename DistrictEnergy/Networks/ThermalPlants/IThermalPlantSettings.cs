using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using DistrictEnergy.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public interface IThermalPlantSettings
    {
        /// <summary>
        /// Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        double F { get; set; }

        /// <summary>
        /// Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        double V { get; set; }

        /// <summary>
        /// Absolute Capacity of the Thermal Plant
        /// </summary>
        [DataMember]
        [DefaultValue(double.PositiveInfinity)]
        double Capacity { get; set; }

        /// <summary>
        /// Name of the Supply Module
        /// </summary>
        String Name { get; set; }

        /// <summary>
        /// Unique Id of the Supply Module
        /// </summary>
        [DataMember]
        Guid Id { get; set; }

        LoadTypes LoadType { get; set; }
        Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
    }
}