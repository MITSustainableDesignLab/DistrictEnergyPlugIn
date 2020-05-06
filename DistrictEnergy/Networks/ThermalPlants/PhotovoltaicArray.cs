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
    internal class PhotovoltaicArray : SolarInput
    {
        public PhotovoltaicArray()
        {
        }

        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_PV { get; set; } = 0;

        /// <summary>
        ///     Cell efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double EFF_PV { get; set; } = 0.15;

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.75)]
        public double UTIL_PV { get; set; } = 0.75;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double LOSS_PV { get; set; } = 0.15;

        [DataMember] [DefaultValue(1313)] public override double F { get; set; } = 1313;
        [DataMember] [DefaultValue(0)] public override double V { get; set; }
        [JsonIgnore] public override double Capacity { get; set; }

        [JsonIgnore]
        public override double CapacityFactor
        {
            get => OFF_PV;
            set => ElectricGenerationViewModel.Instance.OFF_PV = value * 100;
        }

        public override bool IsForced { get; set; }

        [DataMember] [DefaultValue("PV")] public override string Name { get; set; } = "PV";
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType => LoadTypes.Elec;
        public override LoadTypes InputType => LoadTypes.SolarRadiation;

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {LoadTypes.Elec, (1 - LOSS_PV)}
        };

        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];

        public override Dictionary<LoadTypes, SolidColorBrush> Fill
        {
            get =>
                new Dictionary<LoadTypes, SolidColorBrush>
                {
                    {OutputType, new SolidColorBrush(Color.FromRgb(112, 159, 15))}
                };
            set => throw new NotImplementedException();
        }
        /// <summary>
        /// Returns the Array of Global Incidence Radiation (kWh/m2) for a certain range
        /// </summary>
        /// <param name="t">From hour of the year #</param>
        /// <param name="dt">Duration (hours)</param>
        /// <returns></returns>
        public override double[] SolarAvailableInput(int t = 0, int dt = 8760) => DHSimulateDistrictEnergy.Instance.DistrictDemand.RadN.ToList().GetRange(t, dt).ToArray();
    }
}