using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class PipeNetwork : IThermalPlantSettings
    {
        /// <summary>
        ///     Relative distribution heat losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.10)]
        [JsonProperty(Required = Required.Default)]
        public double RelDistHeatLoss { get; set; } = 0.10;

        /// <summary>
        ///     Relative distribution cool losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.05)]
        [JsonProperty(Required = Required.Default)]
        public double RelDistCoolLoss { get; set; } = 0.05;

        /// <summary>
        ///     Should distribution heat losses be accounted for. yes = 1, no = 0
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [JsonProperty(Required = Required.Default)]
        public int UseDistrictLosses { get; set; } = 0;

        [DataMember] [DefaultValue(0)] public double F { get; set; }
        [DataMember] [DefaultValue(0)] public double V { get; set; }
    }
}