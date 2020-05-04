using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class AbsorptionChiller : Dispatchable
    {
        public AbsorptionChiller()
        {
        }

        /// <summary>
        ///     Capacity as percent of peak cooling load (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double OFF_ABS { get; set; } = 0;

        /// <summary>
        ///     Cooling coefficient of performance
        /// </summary>
        [DataMember]
        [DefaultValue(0.90)]
        public double CCOP_ABS { get; set; } = 0.90;

        /// <summary>
        /// Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(633)]
        public override double F { get; set; } = 633;

        /// <summary>
        /// Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0.0004)]
        public override double V { get; set; } = 0.0004;

        public override double Capacity => CalcCapacity();

        private double CalcCapacity()
        {
            if (DistrictControl.Instance is null) return 0;
            return OFF_ABS * DistrictControl.Instance.ListOfDistrictLoads.Where(x => x.LoadType == LoadTypes.Heating).Select(v => v.Input.Max()).Sum();
        }

        [DataMember]
        [DefaultValue("Absorption Chiller")]
        public override string Name { get; set; } = "Absorption Chiller";
        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes OutputType { get; } = LoadTypes.Cooling;
        public override LoadTypes InputType => LoadTypes.Heating;
        public override double CapacityFactor => OFF_ABS;
        public override bool IsForced { get; set; }

        public override Dictionary<LoadTypes, double> ConversionMatrix => new Dictionary<LoadTypes, double>()
        {
            {LoadTypes.Cooling, CCOP_ABS},
            {LoadTypes.Heating, -1}

        };
        public override List<DateTimePoint> Input { get; set; }
        public override List<DateTimePoint> Output { get; set; }
        public override double Efficiency => ConversionMatrix[OutputType];
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(146, 241, 254));
    }
}