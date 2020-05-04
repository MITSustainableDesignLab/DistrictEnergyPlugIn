using System;
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
        public GridGas()
        {
        }
        [DataMember] [DefaultValue(0)] public override double F { get; set; } = 0;

        [DataMember]
        [DefaultValue(0.05)]
        public override double V
        {
            get => UmiContext.Current != null ? UmiContext.Current.ProjectSettings.GasDollars : 0;
            set => UmiContext.Current.ProjectSettings.GasDollars = value;
        }
        public override double Capacity { get; } = double.PositiveInfinity;

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

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>()
        {
            {LoadTypes.Gas, 1}
        };
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(189, 133, 74));
    }
}