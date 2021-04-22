using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.ViewModels;
using Newtonsoft.Json;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class BatteryBank : Storage
    {
        public BatteryBank()
        {
            ConversionMatrix = new Dictionary<LoadTypes, double>
            {
                {LoadTypes.Elec, 1}
            };
        }

        /// <summary>
        ///     Capacity as number of days of autonomy (#)
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public double AUT_BAT { get; set; }

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
        ///     Specific capacity cost per capacity unit f [$/kW]
        /// </summary>
        [DataMember]
        [DefaultValue(1383)]
        public override double F { get; set; } = 1383;

        /// <summary>
        ///     Variable cost per energy unit f [$/kWh]
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public override double V { get; set; }

        [DataMember] [DefaultValue("Battery")] public override string Name { get; set; } = "Battery";
        public override LoadTypes OutputType { get; } = LoadTypes.Elec;
        public override LoadTypes InputType { get; } = LoadTypes.Elec;

        public override double CapacityFactor
        {
            get => AUT_BAT;
            set => ElectricGenerationViewModel.Instance.AUT_BAT = value;
        }

        public override Dictionary<LoadTypes, double> ConversionMatrix { get; set; }

        public override Dictionary<LoadTypes, SolidColorBrush> Fill
        {
            get =>
                new Dictionary<LoadTypes, SolidColorBrush>
                {
                    {OutputType, new SolidColorBrush(Color.FromArgb(200, 231, 71, 126))}
                };
            set => throw new NotImplementedException();
        }

        [JsonIgnore] public override double ChargingEfficiency => 1 - LOSS_BAT;
        [JsonIgnore] public override double DischargingEfficiency => 1 - LOSS_BAT;
        [JsonIgnore] public override double MaxChargingRate => Capacity > 0 ? Capacity / AUT_BAT : 0;
        [JsonIgnore] public override double MaxDischargingRate => Capacity > 0 ? Capacity / AUT_BAT : 0;
        [JsonIgnore] public override double StartingCapacity => BAT_START;
        [JsonIgnore] public override double Capacity { get; set; }
    }
}