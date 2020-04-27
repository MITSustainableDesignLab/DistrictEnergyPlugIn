using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Controls;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class WindTurbine : Dispatchable
    {

        public WindTurbine()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Elec, (1-LOSS_WND)}
            };
        }
        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_WND { get; set; } = 0;

        /// <summary>
        ///     Turbine efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.3)]
        public double EFF_WND { get; set; } = 0.3;

        /// <summary>
        ///     Cut-in speed (m/s)
        /// </summary>
        [DataMember]
        [DefaultValue(5)]
        public double CIN_WND { get; set; } = 5;

        /// <summary>
        ///     Cut-out speed (m/s)
        /// </summary>
        [DataMember]
        [DefaultValue(25)]
        public double COUT_WND { get; set; } = 25;

        /// <summary>
        ///     Rotor area per turbine (m2)
        /// </summary>
        [DataMember]
        [DefaultValue(15)]
        public double ROT_WND { get; set; } = 15;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double LOSS_WND { get; set; } = 0.15;

        [DataMember] [DefaultValue(1347)] public override double F { get; set; } = 1347;
        [DataMember] [DefaultValue(0)] public override double V { get; set; }
        public override double Capacity { get; set; } = 0;

        [DataMember]
        [DefaultValue("Wind Turbines")]
        public override string Name { get; set; } = "Wind Turbines";

        public override Guid Id { get; set; } = Guid.NewGuid();

        public override LoadTypes OutputType { get; set; } = LoadTypes.Elec;
        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(192, 244, 66));
        public override LoadTypes InputType { get; set; } = LoadTypes.Wind;
    }
}