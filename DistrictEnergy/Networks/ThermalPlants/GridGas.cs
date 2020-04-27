﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class GridGas : IDispatchable
    {
        public GridGas()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Gas, 1}
            };
        }
        [DataMember] [DefaultValue(0)] public double F { get; set; } = 0;

        [DataMember]
        [DefaultValue(0.05)]
        public double V
        {
            get => UmiContext.Current != null ? UmiContext.Current.ProjectSettings.GasDollars : 0;
            set => UmiContext.Current.ProjectSettings.GasDollars = value;
        }
        public double Capacity { get; set; } = double.PositiveInfinity;

        [DataMember]
        [DefaultValue("Grid Natural Gas")]
        public string Name { get; set; } = "Grid Natural Gas";

        public Guid Id { get; set; } = Guid.NewGuid();
        public LoadTypes LoadType { get; set; } = LoadTypes.Gas;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public double[] Output { get; set; }
        public double Efficiency => ConversionMatrix[LoadType];
        public SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(189, 133, 74));
    }
}