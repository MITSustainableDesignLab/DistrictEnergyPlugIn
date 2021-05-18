using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.ViewModels;
using LiveCharts.Defaults;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class WindTurbine : WindInput
    {
        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_WND { get; set; }

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
        public override double Capacity { get; set; }

        public override double CapacityFactor
        {
            get => OFF_WND;
            set => ElectricGenerationViewModel.Instance.OFF_WND = value * 100;
        }

        public override bool IsForced { get; set; }

        [DataMember]
        [DefaultValue("Wind Turbines")]
        public override string Name { get; set; } = "Wind Turbines";

        public override Guid Id { get; set; } = Guid.NewGuid();

        public override LoadTypes OutputType => LoadTypes.Elec;
        public override LoadTypes InputType => LoadTypes.Wind;

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>
        {
            {OutputType, 1 - LOSS_WND},
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
                    {OutputType, new SolidColorBrush(Color.FromRgb(192, 244, 66))}
                };
            set => throw new NotImplementedException();
        }

        [JsonIgnore]
        public override double RequiredNumberOfWindTurbines
        {
            get => MaxNumberOfWindTurbines;
            set => ElectricGenerationViewModel.Instance.MaxNumberOfWindTurbines = value * 100;
        }

        public double MaxNumberOfWindTurbines { get; set; }

        public override List<double> WindAvailableInput(int t = 0, int dt = 8760)
        {
            return WindSpeed.ToList().GetRange(t, dt);
        }

        /// <summary>
        ///     Power output [kWh per wind turbine]
        /// </summary>
        /// <param name="t"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public override List<double> PowerPerTurbine(int t = 0, int dt = 8760)
        {
            return WindAvailableInput(t, dt).Where(w => w > CIN_WND && w < COUT_WND)
                .Select(windSpeed => 0.6375 * ROT_WND * Math.Pow(windSpeed, 3) / 1000).ToList();
        }
    }
}