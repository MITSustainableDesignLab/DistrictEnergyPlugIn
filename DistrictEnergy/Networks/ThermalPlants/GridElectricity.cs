using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class GridElectricity : IDispatchable
    {
        public GridElectricity()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Elec, 1}
            };
        }

        [DataMember] [DefaultValue(0)] public double F { get; set; } = 0;

        [DataMember]
        [DefaultValue(0.15)]
        public double V
        {
            get => UmiContext.Current != null ? UmiContext.Current.ProjectSettings.ElectricityDollars : 0;
            set => UmiContext.Current.ProjectSettings.ElectricityDollars = value;
        }

        public double Capacity { get; set; } = double.PositiveInfinity;

        [DataMember]
        [DefaultValue("Grid Electricity")]
        public string Name { get; set; } = "Grid Electricity";

        public Guid Id { get; set; } = Guid.NewGuid();
        public LoadTypes LoadType { get; set; } = LoadTypes.Elec;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Output { get; set; }
        public double Efficiency => ConversionMatrix[LoadType];
        public SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(0, 0, 0));
    }
}