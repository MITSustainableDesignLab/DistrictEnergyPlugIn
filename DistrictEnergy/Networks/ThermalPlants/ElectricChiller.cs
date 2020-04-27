using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class ElectricChiller : Dispatchable
    {
        public ElectricChiller()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Cooling, CCOP_ECH},
                {LoadTypes.Elec, -1}
            };
        }

        /// <summary>
        ///     Cooling coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(4.40)]
        public double CCOP_ECH { get; set; } = 4.40;

        [DataMember] [DefaultValue(164.1)] public override double F { get; set; } = 164.1;
        [DataMember] [DefaultValue(0)] public override double V { get; set; }
        public override double Capacity { get; set; } = double.PositiveInfinity;
        [DataMember] [DefaultValue("Chiller")] public override string Name { get; set; } = "Chiller";
        public override Guid Id { get; set; }
        public override LoadTypes OutputType { get; set; } = LoadTypes.Cooling;
        public override LoadTypes InputType { get; set; } = LoadTypes.Elec;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(93, 153, 170));
    }
}