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
        public override LoadTypes OutputType => LoadTypes.Cooling;
        public override LoadTypes InputType => LoadTypes.Elec;

        public override double CapacityFactor
        {
            get => 1;
            set { }
        }

        public override bool IsForced { get; set; }

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {LoadTypes.Cooling, CCOP_ECH},
            {LoadTypes.Elec, -1}
        };

        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];

        public override Dictionary<LoadTypes, SolidColorBrush> Fill
        {
            get =>
                new Dictionary<LoadTypes, SolidColorBrush>
                {
                    {OutputType, new SolidColorBrush(Color.FromRgb(93, 153, 170))},
                    {InputType, new SolidColorBrush(Color.FromRgb(161,196,206))}
                };
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     0 since carbon comes from Electricity.
        /// </summary>
        public override double CarbonIntensity { get; set; }
    }
}