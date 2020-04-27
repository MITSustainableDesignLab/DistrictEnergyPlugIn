using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using Deedle;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using LiveCharts.Helpers;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class NatGasBoiler : Dispatchable
    {
        public NatGasBoiler()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Heating, EFF_NGB},
                {LoadTypes.Gas, -1}
            };
        }
        /// <summary>
        ///     Heating efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.84)] public double EFF_NGB { get; set; } = 0.84; // (SLD) I thought 70% was quite low

        [DataMember] [DefaultValue(1360)] public override double F { get; set; } = 1360;
        [DataMember] [DefaultValue(0)] public override double V { get; set; }
        public override double Capacity { get; set; } = double.PositiveInfinity;
        [DataMember] [DefaultValue("Natural Gas Boiler")] public override string Name { get; set; } = "Natural Gas Boiler";
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType { get; set; } = LoadTypes.Heating;
        public override LoadTypes InputType { get; set; } = LoadTypes.Gas;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(189, 133, 74));
    }
}