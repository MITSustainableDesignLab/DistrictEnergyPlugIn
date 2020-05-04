using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class WindTurbine : Dispatchable, IWind
    {

        public WindTurbine()
        {
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
        public override double Capacity => CalcCapacity();
        public override double CapacityFactor => OFF_WND;
        public override bool IsForced { get; set; }

        private double CalcCapacity()
        {
            if (DistrictControl.Instance is null) return 0;
            return OFF_WND * DistrictControl.Instance.ListOfDistrictLoads.Where(x => x.LoadType == LoadTypes.Elec)
                .Select(v => v.Input.Sum()).Sum();
        }

        [DataMember]
        [DefaultValue("Wind Turbines")]
        public override string Name { get; set; } = "Wind Turbines";

        public override Guid Id { get; set; } = Guid.NewGuid();

        public override LoadTypes OutputType => LoadTypes.Elec;
        public override LoadTypes InputType => LoadTypes.Wind;
        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>()
        {
            {LoadTypes.Elec, (1-LOSS_WND)}
        };
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(192, 244, 66));

        public List<double> WindAvailableInput(int t=0, int dt=8760)
        {
            return DHSimulateDistrictEnergy.Instance.DistrictDemand.WindN.ToList().GetRange(t, dt);
        }
        public double Power(int t = 0, int dt = 8760) => WindAvailableInput(t, dt).Where(w => w > CIN_WND && w < COUT_WND).Select(w => 0.6375 * ROT_WND * Math.Pow(w, 3) / 1000).Sum() * (1 - LOSS_WND);
        public double NumWnd => Math.Ceiling(Capacity / Power() ); // Divide by 1000 because equation spits out Wh
    }
}