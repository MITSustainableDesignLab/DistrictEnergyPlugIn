using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class BatteryBank : IStorage
    {
        public BatteryBank()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>()
            {
                {LoadTypes.Elec, 1}
            };
        }

        /// <summary>
        ///     Capacity as number of days of autonomy (#)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double AUT_BAT { get; set; } = 0;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)]
        public double LOSS_BAT { get; set; } = 0.15;

        /// <summary>
        ///     The Battery charged state at the beginning of the simulation. Assumed at 0%.
        /// </summary>
        [DataMember]
        [DefaultValue(0.80)]
        public double BAT_START { get; set; } = 0.80;

        /// <summary>
        /// Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(1383)]
        public double F { get; set; } = 1383;

        /// <summary>
        /// Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double V { get; set; } = 0;

        public double Capacity { get; set; } = 0;
        [DataMember] [DefaultValue("Battery")] public string Name { get; set; } = "Battery";
        public Guid Id { get; set; } = new Guid();
        public LoadTypes OutputType { get; set; } = LoadTypes.Elec;
        public LoadTypes InputType { get; set; } = LoadTypes.Elec;
        public Dictionary<LoadTypes, double> ConversionMatrix { get; set; }
        public List<DateTimePoint> Input { get; set; }
        public List<DateTimePoint> Output { get; set; }
        public double Efficiency => ConversionMatrix[OutputType];
        public SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(231, 71, 126));
        public double ChargingEfficiency => 1 - LOSS_BAT;
        public double DischargingEfficiency => 1 - LOSS_BAT;
        public double StorageStandingLosses { get; set; } = 0.001;
        public List<DateTimePoint> Storage { get; set; }
        public double MaxChargingRate => Capacity > 0 ? Capacity / AUT_BAT : 0;
        public double MaxDischargingRate => Capacity > 0 ? Capacity / AUT_BAT : 0;
        public double StartingCapacity => Capacity * BAT_START;
    }
}