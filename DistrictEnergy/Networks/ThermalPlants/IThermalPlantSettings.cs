using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Newtonsoft.Json;

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
        /// Absolute Capacity of the Supply Module (kW of LoadType)
        /// </summary>
        [DataMember]
        [DefaultValue(double.PositiveInfinity)]
        double Capacity { get; }

        /// <summary>
        /// Name of the Supply Module
        /// </summary>
        String Name { get; set; }

        /// <summary>
        /// Unique Id of the Supply Module
        /// </summary>
        [DataMember]
        Guid Id { get; set; }

        LoadTypes OutputType { get; }
        [JsonIgnore] Dictionary<LoadTypes, double> ConversionMatrix { get; }

        /// <summary>
        /// Input Energy to the Supply Module per period
        /// </summary>
        [JsonIgnore]
        List<DateTimePoint> Input { get; set; }

        /// <summary>
        /// Output Energy of the Supply Module per period
        /// </summary>
        [JsonIgnore]
        List<DateTimePoint> Output { get; set; }

        /// <summary>
        /// Fill Used to Color Supply Module
        /// </summary>
        [DataMember]
        SolidColorBrush Fill { get; set; }

        [JsonIgnore] LoadTypes InputType { get; }
        [JsonIgnore] GraphCost FixedCost { get; }
        [JsonIgnore] GraphCost VariableCost { get; }
        [JsonIgnore] double TotalCost { get; }
        [JsonIgnore] double CapacityFactor { get; }
    }
}