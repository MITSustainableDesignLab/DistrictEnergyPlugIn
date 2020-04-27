using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class SolarThermalCollector : IDispatchable, ISolar
    {
        public SolarThermalCollector()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Heating, EFF_SHW * (1-LOSS_SHW) * UTIL_SHW}
            };
        }
        /// <summary>
        ///     Target offset as percent of annual energy (%) Todo: Annual Energy?
        /// </summary>
        [DataMember]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(0)]
        public double OFF_SHW { get; set; } = 0;

        /// <summary>
        ///     Collector efficiency (%)
        /// </summary>
        [DataMember]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(0.45)]
        public double EFF_SHW { get; set; } = 0.45;

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        [DataMember]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(0.75)]
        public double UTIL_SHW { get; set; } = 0.75;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(0.15)]
        public double LOSS_SHW { get; set; } = 0.15;

        [DataMember] [DefaultValue(7191)] public double F { get; set; } = 7191;
        [DataMember] [DefaultValue(0.00887)] public double V { get; set; } = 0.00887;
        public double Capacity { get; set; } = 0;

        [DataMember]
        [DefaultValue("Solar Thermal")]
        public string Name { get; set; } = "Solar Thermal";

        public Guid Id { get; set; } = Guid.NewGuid();

        public LoadTypes OutputType { get; set; } = LoadTypes.Heating;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Input { get; set; }
        public double[] Output { get; set; }
        public double Efficiency => ConversionMatrix[LoadTypes.Heating];
        public SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(251, 209, 39));
        public LoadTypes InputType { get; set; } = LoadTypes.SolarRadiation;
        public double AvailableArea => Capacity / (SolarAvailableInput.Sum() * EFF_SHW * (1 - LOSS_SHW) * UTIL_SHW);
        public double[] SolarAvailableInput => DHSimulateDistrictEnergy.Instance.DistrictDemand.RadN;
    }
}