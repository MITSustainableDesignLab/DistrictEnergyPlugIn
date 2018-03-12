using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Controls;

namespace DistrictEnergy.Networks.ThermalPlants
{
    internal class WindTurbine : IThermalPlantSettings
    {
        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.05)] public double OFF_WND { get; set; } = 0.05;

        /// <summary>
        ///     Turbine efficiency (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.3)] public double EFF_WND { get; set; } = 0.3;

        /// <summary>
        ///     Cut-in speed (m/s)
        /// </summary>
        [DataMember]
        [DefaultValue(5)] public double CIN_WND { get; set; } = 5;

        /// <summary>
        ///     Cut-out speed (m/s)
        /// </summary>
        [DataMember]
        [DefaultValue(25)] public double COUT_WND { get; set; } = 25;

        /// <summary>
        ///     Rotor area per turbine (m2)
        /// </summary>
        [DataMember]
        [DefaultValue(15)] public double ROT_WND { get; set; } = 15;

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        [DataMember]
        [DefaultValue(0.15)] public double LOSS_WND { get; set; } = 0.15;
    }

    public class DoubleRangeRule : ValidationRule
    {
        public double Min { get; set; }

        public double Max { get; set; }

        public override ValidationResult Validate(object value,
            CultureInfo cultureInfo)
        {
            double parameter = 0;

            try
            {
                if (((string)value).Length > 0)
                {
                    parameter = Double.Parse((String)value);
                }
            }
            catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or "
                                                   + e.Message);
            }

            if ((parameter < this.Min) || (parameter > this.Max))
            {
                return new ValidationResult(false,
                    "Please enter value in the range: "
                    + this.Min + " - " + this.Max + ".");
            }
            return new ValidationResult(true, null);
        }
    }
}