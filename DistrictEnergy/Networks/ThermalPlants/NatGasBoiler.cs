using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public class NatGasBoiler : Dispatchable
    {
        /// <summary>
        ///     Heating efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.84)]
        public double EFF_NGB { get; set; } = 0.84; // (SLD) I thought 70% was quite low

        [DataMember] [DefaultValue(1360)] public override double F { get; set; } = 1360;
        [DataMember] [DefaultValue(0)] public override double V { get; set; }
        public override double Capacity { get; set; } = double.PositiveInfinity;

        [DataMember]
        [DefaultValue("Natural Gas Boiler")]
        public override string Name { get; set; } = "Natural Gas Boiler";

        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType => LoadTypes.Heating;
        public override LoadTypes InputType => LoadTypes.Gas;

        public override double CapacityFactor
        {
            get => 1;
            set { }
        }

        public override bool IsForced { get; set; }

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {OutputType, EFF_NGB},
            {InputType, -1}
        };

        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];

        public override Dictionary<LoadTypes, SolidColorBrush> Fill
        {
            get =>
                new Dictionary<LoadTypes, SolidColorBrush>
                {
                    {OutputType, new SolidColorBrush(Color.FromRgb(189, 133, 74))}
                };
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     0 since carbon comes from GridGas.
        /// </summary>
        public override double CarbonIntensity { get; set; } = 0;
    }
}