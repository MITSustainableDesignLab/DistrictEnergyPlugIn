using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DistrictEnergy.ViewModels;
using EnergyPlusWeather;
using Mit.Umi.RhinoServices.Context;
using Rhino;
using Rhino.Commands;

namespace DistrictEnergy
{
    [Guid("929185AA-DB2C-4AA5-B1C0-E89C93F0704D")]
    public class DHSimulateDistrictEnergy : Command
    {
        private readonly double[] HW_ABS = new double[8760];

        public DHSimulateDistrictEnergy()
        {
            Instance = this;
        }

        ///<summary>The only instance of the DHSimulateDistrictEnergy command.</summary>
        public static DHSimulateDistrictEnergy Instance { get; private set; }

        public override string EnglishName => "DHSimulateDistrictEnergy";

        private double[] CHW_n { get; set; }
        private double[] HW_n { get; set; }
        private double[] ELEC_n { get; set; }
        private decimal[] RAD_n { get; set; }
        private decimal[] WIND_n { get; set; }


        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var umiContext = UmiContext.Current;
            if (umiContext == null)
            {
                RhinoApp.WriteLine("Problem getting the umi context");
                return Result.Failure;
            }

            // Getting the Load Curve for all buildings

            CHW_n = GetHourlyChilledWaterProfile(umiContext);
            HW_n = GetHourlyHotWaterLoadProfile(umiContext).ToArray();
            ELEC_n = GetHourlyElectricalLoadProfile(umiContext).ToArray();
            RAD_n = GetHourlyLocationSolarRadiation(umiContext).ToArray();
            WIND_n = GetHourlyLocationWind(umiContext).ToArray();

            RhinoApp.WriteLine(
                $"Calculated...\n{CHW_n.Length} datapoints for ColdWater profile\n{HW_n.Count()} datapoints for HotWater\n{ELEC_n.Count()} datapoints for Electricity\n{RAD_n.Count()} datapoints for Solar Frad\n{WIND_n.Count()} datapoints for WindSpeed");
            CalculateHW_ABS();
            RhinoApp.WriteLine($"HW_ABS = {HW_ABS}");
            return Result.Success;
        }

        private double[] GetHourlyChilledWaterProfile(UmiContext context)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating cooling loads");
            var nbDataPoint = context.GetObjects().Select(b => b.Data["SDL/Cooling"].Data.Count).ToList()[0];
            var a = new double[nbDataPoint];
            foreach (var umiObject in context.GetObjects())
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var d = umiObject.Data["SDL/Cooling"].Data[i];
                    a[i] += d;
                }

            return a;
        }

        private IEnumerable<double> GetHourlyHotWaterLoadProfile(UmiContext context)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating hot water loads");
            var nbDataPoint = context.GetObjects().Select(b => b.Data["SDL/Heating"].Data.Count).ToList()[0];
            return Enumerable.Range(0, nbDataPoint)
                .Select(i => context.GetObjects().Select(b =>
                    b.Data["SDL/Heating"].Data
                        .Zip(b.Data["SDL/Domestic Hot Water"].Data, (heating, dhw) => heating + dhw).ToList()[i]
                ).Sum());
        }

        private IEnumerable<double> GetHourlyElectricalLoadProfile(UmiContext context)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating electrical loads: SDL/Equipment + SDL/Lighting");
            var nbDataPoint = context.GetObjects().Select(b => b.Data["SDL/Equipment"].Data.Count).ToList()[0];
            return Enumerable.Range(0, nbDataPoint)
                .Select(i => context.GetObjects().Select(b =>
                        b.Data["SDL/Equipment"].Data
                            .Zip(b.Data["SDL/Lighting"].Data, (equipment, lighting) => equipment + lighting)
                            .ToList()[i])
                    .Sum());
        }

        private IEnumerable<decimal> GetHourlyLocationSolarRadiation(UmiContext context)
        {
            RhinoApp.WriteLine("Calculating Solar Radiation on horizontal surface");
            var a = new EPWeatherData();
            a.GetRawData(context.WeatherFilePath);
            return a.HourlyWeatherDataRawList.Select(b => b.GHorRadiation);
        }

        private IEnumerable<decimal> GetHourlyLocationWind(UmiContext context)
        {
            RhinoApp.WriteLine("Calculating wind for location");
            var a = new EPWeatherData();
            a.GetRawData(context.WeatherFilePath);
            return a.HourlyWeatherDataRawList.Select(b => b.WindSpeed);
        }

        private void CalculateHW_ABS()
        {
            if (CHW_n.Length > 0)
            {
                var CAP_ABS = CHW_n.Max() * OFF_ABS;
                for (var i = 0; 0 < CHW_n.Length; i++) HW_ABS[i] = Math.Min(CHW_n[i], CAP_ABS) / CCOP_ABS;
            }
        }

        #region AvailableSettings

        private double CCOP_ECH { get; } = PlantSettingsViewModel.Instance.CCOP_ECH;
        private double EFF_NGB { get; } = PlantSettingsViewModel.Instance.EFF_NGB;
        private double OFF_ABS { get; } = PlantSettingsViewModel.Instance.OFF_ABS;
        private double CCOP_ABS { get; } = PlantSettingsViewModel.Instance.CCOP_ABS;
        private double AUT_BAT { get; } = PlantSettingsViewModel.Instance.AUT_BAT;
        private double LOSS_BAT { get; } = PlantSettingsViewModel.Instance.LOSS_BAT;
        private string TMOD_CHP { get; } = PlantSettingsViewModel.Instance.TMOD_CHP.ToString();
        private double OFF_CHP { get; } = PlantSettingsViewModel.Instance.OFF_CHP;
        private double EFF_CHP { get; } = PlantSettingsViewModel.Instance.EFF_CHP;
        private double HREC_CHP { get; } = PlantSettingsViewModel.Instance.HREC_CHP;
        private double OFF_EHP { get; } = PlantSettingsViewModel.Instance.OFF_EHP;
        private double HCOP_EHP { get; } = PlantSettingsViewModel.Instance.HCOP_EHP;
        private double AUT_HWT { get; } = PlantSettingsViewModel.Instance.AUT_HWT;
        private double LOSS_HWT { get; } = PlantSettingsViewModel.Instance.LOSS_HWT;
        private double OFF_PV { get; } = PlantSettingsViewModel.Instance.OFF_PV;
        private double UTIL_PV { get; } = PlantSettingsViewModel.Instance.UTIL_PV;
        private double LOSS_PV { get; } = PlantSettingsViewModel.Instance.LOSS_PV;
        private double EFF_SHW { get; } = PlantSettingsViewModel.Instance.EFF_SHW;
        private double LOSS_SHW { get; } = PlantSettingsViewModel.Instance.LOSS_SHW;
        private double OFF_SHW { get; } = PlantSettingsViewModel.Instance.OFF_SHW;
        private double UTIL_SHW { get; } = PlantSettingsViewModel.Instance.UTIL_SHW;
        private double CIN_WND { get; } = PlantSettingsViewModel.Instance.CIN_WND;
        private double COP_WND { get; } = PlantSettingsViewModel.Instance.COP_WND;
        private double COUT_WND { get; } = PlantSettingsViewModel.Instance.COUT_WND;
        private double OFF_WND { get; } = PlantSettingsViewModel.Instance.OFF_WND;
        private double ROT_WND { get; } = PlantSettingsViewModel.Instance.ROT_WND;

        #endregion
    }
}