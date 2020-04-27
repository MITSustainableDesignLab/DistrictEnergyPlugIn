using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class AbsorptionChiller : Dispatchable
    {
        public AbsorptionChiller()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Cooling, CCOP_ABS},
                {LoadTypes.Heating, -1}

            };
        }

        /// <summary>
        ///     Capacity as percent of peak cooling load (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_ABS { get; set; } = 0;

        /// <summary>
        ///     Cooling coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(0.90)]
        public double CCOP_ABS { get; set; } = 0.90;

        /// <summary>
        /// Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(633)]
        public override double F { get; set; } = 633;

        /// <summary>
        /// Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0.0004)]
        public override double V { get; set; } = 0.0004;

        public override double Capacity { get; set; } = 0;

        [DataMember]
        [DefaultValue("Absorption Chiller")]
        public override string Name { get; set; } = "Absorption Chiller";
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType { get; set; } = LoadTypes.Cooling;
        public override LoadTypes InputType { get; set; } = LoadTypes.Heating;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(146, 241, 254));
    }
}