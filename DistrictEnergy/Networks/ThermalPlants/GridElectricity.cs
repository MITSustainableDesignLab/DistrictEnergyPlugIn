using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;
using Newtonsoft.Json;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class GridElectricity : Dispatchable
    {
        [DataMember] [DefaultValue(0)] public override double F { get; set; }

        [DataMember]
        [DefaultValue(0.15)]
        public override double V
        {
            get => UmiContext.Current != null ? UmiContext.Current.ProjectSettings.ElectricityDollars : 0;
            set => UmiContext.Current.ProjectSettings.ElectricityDollars = value;
        }

        /// <summary>
        /// Carbon intensity [gCO2eq/kWh] from https://www.iea.org/reports/global-energy-co2-status-report-2019/emissions
        /// </summary>
        [JsonIgnore]
        public override double CarbonIntensity
        {
            get
            {
                if (UmiContext.Current != null) return UmiContext.Current.ProjectSettings.ElectricityCarbon * 1000;
                return 475;
            }
            set
            {
                if (UmiContext.Current != null) UmiContext.Current.ProjectSettings.ElectricityCarbon = value;
            }
        }

        public override double Capacity { get; set; } = double.PositiveInfinity;

        [DataMember]
        [DefaultValue("Grid Electricity")]
        public override string Name { get; set; } = "Grid Electricity";

        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType => LoadTypes.Elec;
        public override LoadTypes InputType => LoadTypes.GridElec;

        public override double CapacityFactor
        {
            get => 1;
            set { }
        }

        public override bool IsForced { get; set; }

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {OutputType, 1},
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
                    {OutputType, new SolidColorBrush(Color.FromRgb(0, 0, 0))}
                };
            set => throw new NotImplementedException();
        }
    }
}