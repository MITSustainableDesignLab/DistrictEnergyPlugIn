using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.ViewModels;
using LiveCharts.Defaults;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class SolarThermalCollector : SolarInput
    {
        public SolarThermalCollector()
        {
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

        [DataMember] [DefaultValue(7191)] public override double F { get; set; } = 7191;
        [DataMember] [DefaultValue(0.00887)] public override double V { get; set; } = 0.00887;
        public override double Capacity { get; set; }

        public override double CapacityFactor
        {
            get => OFF_SHW;
            set => HotWaterViewModel.Instance.OFF_SHW = value * 100;
        }

        public override bool IsForced { get; set; }

        [DataMember]
        [DefaultValue("Solar Thermal")]
        public override string Name { get; set; } = "Solar Thermal";

        public override Guid Id { get; set; } = Guid.NewGuid();

        public override LoadTypes OutputType => LoadTypes.Heating;
        public override LoadTypes InputType => LoadTypes.SolarRadiation;

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>()
        {
            {LoadTypes.Heating, EFF_SHW * (1 - LOSS_SHW) * UTIL_SHW}
        };

        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[LoadTypes.Heating];

        public override Dictionary<LoadTypes, SolidColorBrush> Fill
        {
            get =>
                new Dictionary<LoadTypes, SolidColorBrush>
                {
                    {OutputType, new SolidColorBrush(Color.FromRgb(251, 209, 39))}
                };
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the Array of Global Incidence Radiation (kWh/m2) for a certain range
        /// </summary>
        /// <param name="t">From hour of the year #</param>
        /// <param name="dt">Duration (hours)</param>
        /// <returns></returns>
        public override double[] SolarAvailableInput(int t = 0, int dt = 8760) => DHSimulateDistrictEnergy.Instance
            .DistrictDemand.RadN.ToList().GetRange(t, dt).ToArray();
    }
}