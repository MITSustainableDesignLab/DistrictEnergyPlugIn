﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class GridGas : Dispatchable
    {
        [DataMember] [DefaultValue(0)] public override double F { get; set; }

        [DataMember]
        [DefaultValue(0.05)]
        public override double V
        {
            get => UmiContext.Current != null ? UmiContext.Current.ProjectSettings.GasDollars : 0;
            set { }
        }

        /// <summary>
        ///     The CO2 generated by burning natural gas is 185 g / kWh https://www.carbonindependent.org/15.html
        /// </summary>
        [DataMember]
        [DefaultValue(185)]
        public override double CarbonIntensity
        {
            get => UmiContext.Current != null ? UmiContext.Current.ProjectSettings.GasCarbon * 1000 : 185;
            set { }
        }

        public override double Capacity { get; set; } = double.PositiveInfinity;

        [DataMember]
        [DefaultValue("Grid Natural Gas")]
        public override string Name { get; set; } = "Grid Natural Gas";

        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType => LoadTypes.Gas;
        public override LoadTypes InputType => LoadTypes.GridGas;

        public override double CapacityFactor
        {
            get => 1;
            set { }
        }

        public override bool IsForced { get; set; }

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {LoadTypes.Gas, 1}
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
    }
}