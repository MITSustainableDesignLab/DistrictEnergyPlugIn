using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DistrictEnergy.ViewModels;
using EnergyPlusWeather;
using Mit.Umi.RhinoServices.Context;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace DistrictEnergy
{
    [Guid("929185AA-DB2C-4AA5-B1C0-E89C93F0704D")]
    public class DHSimulateDistrictEnergy : Command
    {
        private int _progressBarPos;

        public DHSimulateDistrictEnergy()
        {
            Instance = this;
        }

        ///<summary>The only instance of the DHSimulateDistrictEnergy command.</summary>
        public static DHSimulateDistrictEnergy Instance { get; private set; }

        public override string EnglishName => "DHSimulateDistrictEnergy";

        /// <summary>
        ///     Hourly chilled water load profile (kWh)
        /// </summary>
        private static double[] CHW_n { get; set; }

        /// <summary>
        ///     Hourly hot water load profile (kWh)
        /// </summary>
        private static double[] HW_n { get; set; }

        /// <summary>
        ///     Hourly electricity load profile (kWh)
        /// </summary>
        private double[] ELEC_n { get; set; }

        /// <summary>
        ///     Hourly location solar radiation data (kWh/m2)
        /// </summary>
        private decimal[] RAD_n { get; set; }

        /// <summary>
        ///     Hourly location wind speed data (m/s)
        /// </summary>
        private decimal[] WIND_n { get; set; }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var umiContext = UmiContext.Current;
            if (umiContext == null)
            {
                RhinoApp.WriteLine("Problem getting the umi context");
                return Result.Failure;
            }

            _progressBarPos = 0;
            // Getting the Load Curve for all buildings
            CHW_n = GetHourlyChilledWaterProfile(umiContext);
            HW_n = GetHourlyHotWaterLoadProfile(umiContext);
            ELEC_n = GetHourlyElectricalLoadProfile(umiContext).ToArray();
            RAD_n = GetHourlyLocationSolarRadiation(umiContext).ToArray();
            WIND_n = GetHourlyLocationWind(umiContext).ToArray();
            StatusBar.HideProgressMeter();

            RhinoApp.WriteLine(
                $"Calculated...\n{CHW_n.Length} datapoints for ColdWater profile\n{HW_n.Count()} datapoints for HotWater\n{ELEC_n.Count()} datapoints for Electricity\n{RAD_n.Count()} datapoints for Solar Frad\n{WIND_n.Count()} datapoints for WindSpeed");
            eqHW_ABS();
            RhinoApp.WriteLine($"HW_ABS = {HW_ABS}");
            return Result.Success;
        }

        private double[] GetHourlyChilledWaterProfile(UmiContext context)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating cooling loads");
            var nbDataPoint = context.GetObjects().Select(b => b.Data["SDL/Cooling"].Data.Count).Max();
            var a = new double[nbDataPoint];
            StatusBar.HideProgressMeter();
            StatusBar.ShowProgressMeter(0, nbDataPoint * context.GetObjects().Count * 3,
                "Aggregating Cooling Loads", true, true);

            foreach (var umiObject in context.GetObjects())
            {
                if (umiObject.Data["SDL/Cooling"].Data.Count != 8760) continue;
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var d = umiObject.Data["SDL/Cooling"].Data[i];
                    a[i] += d;
                    _progressBarPos += 1;
                    StatusBar.UpdateProgressMeter(_progressBarPos, true);
                }
            }

            return a;
        }

        private double[] GetHourlyHotWaterLoadProfile(UmiContext context)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating hot water loads");
            var nbDataPoint = context.GetObjects().Select(b => b.Data["SDL/Cooling"].Data.Count).Max();
            var a = new double[nbDataPoint];
            StatusBar.HideProgressMeter();
            StatusBar.ShowProgressMeter(0, nbDataPoint * context.GetObjects().Count * 3,
                "Aggregating Hot Water Loads", true, true);
            foreach (var umiObject in context.GetObjects())
            {
                if (umiObject.Data["SDL/Heating"].Data.Count != 8760) continue;
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var d = umiObject.Data["SDL/Heating"].Data[i] + umiObject.Data["SDL/Domestic Hot Water"].Data[i];
                    a[i] += d;
                    _progressBarPos += 1;
                    StatusBar.UpdateProgressMeter(_progressBarPos, true);
                }
            }

            return a;
        }

        private double[] GetHourlyElectricalLoadProfile(UmiContext context)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating electrical loads: SDL/Equipment + SDL/Lighting");
            var nbDataPoint = context.GetObjects().Select(b => b.Data["SDL/Cooling"].Data.Count).Max();
            var a = new double[nbDataPoint];
            StatusBar.HideProgressMeter();
            StatusBar.ShowProgressMeter(0, nbDataPoint * context.GetObjects().Count * 3,
                "Aggregating Hot Water Loads", true, true);
            foreach (var umiObject in context.GetObjects())
            {
                if (umiObject.Data["SDL/Equipment"].Data.Count != 8760) continue;
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var d = umiObject.Data["SDL/Equipment"].Data[i] + umiObject.Data["SDL/Lighting"].Data[i];
                    a[i] += d;
                    _progressBarPos += 1;
                    StatusBar.UpdateProgressMeter(_progressBarPos, true);
                }
            }

            return a;
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

        /// <summary>
        ///     Equation 19 : Hourly purchased grid Wlectricity for whole site
        /// </summary>
        private void eqELEC_proj()
        {
            for (var i = 0; i < ELEC_n.Length; i++) ELEC_proj[i] = ELEC_n[i] - ELEC_REN[i] - ELEC_BAT[i] - ELEC_CHP[i];
        }

        /// <summary>
        ///     Equation 20 : Hourly purchased Natural Gas for whole site
        /// </summary>
        private void eqNGAS_proj()
        {
            for (var i = 0; i < ELEC_n.Length; i++) NGAS_proj[i] = NGAS_NGB[i] + NGAS_CHP[i];
        }

        #region Constants

        /// <summary>
        ///     Cooling capacity of absorption chillers (kW)
        /// </summary>
        private double CAP_ABS { get; } = CHW_n.Max() * OFF_ABS;

        /// <summary>
        ///     Capacity of Electrical Heat Pumps
        /// </summary>
        private double CAP_EHP { get; } = HW_n.Max() * OFF_EHP;

        /// <summary>
        ///     Capacity of Battery
        /// </summary>
        private double CAP_BAT { get; } = 0; // todo CAP_BAT eqaution

        /// <summary>
        ///     Tank capacity
        /// </summary>
        private double CAP_HWT { get; } = 0; // todo : CAP_HWT equation

        /// <summary>
        ///     Capacity of CHP plant
        /// </summary>
        private double CAP_CHP { get; } = 0; // todo : CAP_CHP equation

        /// <summary>
        ///     Calculated required area of solar thermal collector
        /// </summary>
        private double AREA_SHW { get; } = 0; // todo : Area equation

        /// <summary>
        ///     Calculated required area of PV collectors
        /// </summary>
        private double AREA_PV { get; } = 0; // todo : Area equation

        /// <summary>
        ///     Number of turbines needed
        /// </summary>
        private double NUM_WND { get; } = 0; // todo : Number of wind turbines needed

        /// <summary>
        ///     Dischrage rate of thermal tank
        /// </summary>
        private double DCHG_HWT { get; } =
            0; // todo Discharge rate is said to be a user defined parameter but I think it should be calculated from the number of days of autonomy (SLD)

        /// <summary>
        ///     Discharge rate of battery
        /// </summary>
        private double DCHG_BAT { get; } =
            0; // todo Discharge rate is said to be a user defined parameter but I think it should be calculated from the number of days of autonomy (SLD)

        #endregion

        #region Equation 1 to 18

        /// <summary>
        ///     Equation 1
        /// </summary>
        private void eqHW_ABS()
        {
            for (var i = 0; 0 < CHW_n.Length; i++) HW_ABS[i] = Math.Min(CHW_n[i], CAP_ABS) / CCOP_ABS;
        }

        /// <summary>
        ///     Equation 2
        /// </summary>
        private void eqELEC_ECH()
        {
            for (var i = 0; i < CHW_n.Length; i++)
                if (CHW_n[i] > CAP_ABS)
                    ELEC_ECH[i] = (CHW_n[i] - CAP_ABS) / CCOP_ECH;
        }

        /// <summary>
        ///     Equation 3 : The electricity consumption required to generate hot water from heat pumps
        /// </summary>
        private void eqELEC_EHP()
        {
            for (var i = 0; i < HW_n.Length; i++) ELEC_EHP[i] = Math.Min(HW_n[i], CAP_EHP) / HCOP_EHP;
        }

        /// <summary>
        ///     Equation 4 : The annual boiler natural gas consumption to generate project hot water
        /// </summary>
        private void eqNGAS_NGB()
        {
            for (var i = 0; i < HW_n.Length; i++)
                NGAS_NGB[i] = (HW_n[i] - HW_EHP[i] + HW_ABS[i] - HW_SHW[i] - HW_HWT[i] - HW_CHP[i]) / EFF_NGB;
        }

        /// <summary>
        ///     Equation 5 : The annual total solar hot water generation to meet building loads
        /// </summary>
        private void eqHW_SHW()
        {
            for (var i = 0; i < RAD_n.Length; i++)
                HW_SHW[i] = Math.Min((double) RAD_n[i] * AREA_SHW * EFF_SHW * UTIL_SHW * LOSS_SHW, HW_n[i] - HW_EHP[i]);
        }

        /// <summary>
        ///     Equation 6 : The tank charge for each hour
        /// </summary>
        private void eqTANK_CHG_n()
        {
            TANK_CHG_n[0] = 0.5; // todo Assumed tank is empty at beginning of sumulation
            for (var i = 1; i < TANK_CHG_n.Length; i++)
                TANK_CHG_n[i] = Math.Min(TANK_CHG_n[i - 1] + SUR_n[i] - DEF_n[i], CAP_HWT);
        }

        /// <summary>
        ///     Equation 7 : The annual demand met by hot water tanks
        /// </summary>
        private void eqHW_HWT()
        {
            for (var i = 0; i < TANK_CHG_n.Length; i++) HW_HWT[i] = Math.Min(TANK_CHG_n[i], DCHG_HWT);
        }

        /// <summary>
        ///     Equation 8 : The PV total electricity generation
        /// </summary>
        private void eqELEC_PV()
        {
            for (var i = 0; i < RAD_n.Length; i++)
                ELEC_PV[i] = (double) RAD_n[i] * AREA_PV * EFF_PV * UTIL_PV * LOSS_PV;
        }

        /// <summary>
        ///     Equation 9 : The annual electricity generation for Wind Turbines
        /// </summary>
        private void eqELEC_WND()
        {
            for (var i = 0; i < WIND_n.Length; i++)
                ELEC_WND[i] = 0.6375 * Math.Pow((double) WIND_n[i], 3) * ROT_WND * NUM_WND * COP_WND * LOSS_WND;
        }

        /// <summary>
        ///     Equation 10 : The total renewable electricity
        /// </summary>
        private void eqELEC_REN()
        {
            for (var i = 0; i < ELEC_n.Length; i++) ELEC_REN[i] = Math.Min(ELEC_PV[i] + ELEC_WND[i], ELEC_n[i]);
        }

        /// <summary>
        ///     Equation 11 : The battery charge for each hour
        /// </summary>
        private void eqBAT_CHG_n()
        {
            BAT_CHG_n[0] = 0; // todo Assumed BAT is empty at beginning of sumulation
            for (var i = 1; i < BAT_CHG_n.Length; i++)
                BAT_CHG_n[i] = Math.Min(BAT_CHG_n[i - 1] + SUR_n[i] - DEF_n[i], CAP_BAT);
        }

        /// <summary>
        ///     Equation 12 : The annual demand met by the battery bank
        /// </summary>
        private void eqELEC_BAT()
        {
            for (var i = 0; i < BAT_CHG_n.Length; i++) ELEC_BAT[i] = Math.Min(BAT_CHG_n[i], DCHG_BAT);
        }

        /// <summary>
        ///     Equation  13/18 : The annual heating energy recovered from the combined heat and power plant and supplied to the
        ///     project
        /// </summary>
        private void eqHW_CHP(string tracking)
        {
            for (var i = 0; i < HW_n.Length; i++)
            {
                if (string.Equals(tracking, "Thermal"))
                    HW_CHP[i] = Math.Min(CAP_CHP / EFF_CHP * HREC_CHP,
                        HW_n[i] - HW_EHP[i] + HW_ABS[i] - HW_SHW[i] - HW_HWT[i]);
                if (string.Equals(tracking, "Electrical"))
                    HW_CHP[i] = NGAS_CHP[i] * HREC_CHP;
            }
        }

        /// <summary>
        ///     Equation 14/17 : The natural gas consumed by the CHP plant
        /// </summary>
        /// <param name="tracking">The tracking mode of the CHP plant (converted to a string : eg "Thermal" of "Electrical")</param>
        private void eqNGAS_CHP(string tracking)
        {
            for (var i = 0; i < HW_CHP.Length; i++)
            {
                if (string.Equals(tracking, "Thermal"))
                    NGAS_CHP[i] = HW_CHP[i] / HREC_CHP;
                if (string.Equals(tracking, "Electrical"))
                    NGAS_CHP[i] = ELEC_CHP[i] / EFF_CHP;
            }
        }

        /// <summary>
        ///     Equation 15/16 : The the electricity generated by the CHP plant
        /// </summary>
        /// <param name="tracking"></param>
        private void eqELEC_CHP(string tracking)
        {
            for (var i = 0; i < NGAS_CHP.Length; i++)
            {
                if (string.Equals(tracking, "Thermal"))
                    ELEC_CHP[i] = NGAS_CHP[i] * EFF_CHP;
                if (string.Equals(tracking, "Electrical"))
                    ELEC_CHP[i] = Math.Min(CAP_CHP, ELEC_n[i] - ELEC_REN[i] - ELEC_BAT[i]);
            }
        }

        #endregion

        #region Results Array

        /// <summary>
        ///     eq11 The battery charge for each hour
        /// </summary>
        private readonly double[] BAT_CHG_n = new double[8760];

        /// <summary>
        ///     eq12 Demand met by the battery bank
        /// </summary>
        private readonly double[] ELEC_BAT = new double[8760];

        /// <summary>
        ///     eq15/16 Electricity generated by CHP plant
        /// </summary>
        private readonly double[] ELEC_CHP = new double[8760];

        /// <summary>
        ///     eq2 Electricity Consumption to generate chilled water from chillers
        /// </summary>
        private readonly double[] ELEC_ECH = new double[8760];

        /// <summary>
        ///     eq3 Electricity consumption required to generate hot water from HPs
        /// </summary>
        private readonly double[] ELEC_EHP = new double[8760];

        /// <summary>
        ///     eq8 Total PV electricity generation
        /// </summary>
        private readonly double[] ELEC_PV = new double[8760];

        /// <summary>
        ///     eq10 Total nenewable electricity generation
        /// </summary>
        private readonly double[] ELEC_REN = new double[8760];

        /// <summary>
        ///     eq9 Total Wind electricity generation
        /// </summary>
        private readonly double[] ELEC_WND = new double[8760];

        /// <summary>
        ///     eq1 Hot water required for Absorption Chiller
        /// </summary>
        private readonly double[] HW_ABS = new double[8760];

        /// <summary>
        ///     Hot water met by electric heat pumps
        /// </summary>
        private readonly double[] HW_EHP = new double[8760];

        /// <summary>
        ///     eq13 Heating energy recovered from the combined heat and power plant and supplied to the project
        /// </summary>
        private readonly double[] HW_CHP = new double[8760];

        /// <summary>
        ///     eq7 Demand met by hot water tanks
        /// </summary>
        private readonly double[] HW_HWT = new double[8760];

        /// <summary>
        ///     eq5 Total Solar Hot Water generation to meet building loads
        /// </summary>
        private readonly double[] HW_SHW = new double[8760];

        /// <summary>
        ///     eq14/17 Natural gas consumed by CHP plant
        /// </summary>
        private readonly double[] NGAS_CHP = new double[8760];

        /// <summary>
        ///     eq4 Boiler natural gas consumption to generate project hot water
        /// </summary>
        private readonly double[] NGAS_NGB = new double[8760];

        /// <summary>
        ///     eq6 The tank charge for each hour [kWh]
        /// </summary>
        private readonly double[] TANK_CHG_n = new double[8760];

        /// <summary>
        ///     Hour Surplus todo What's the equation ?
        /// </summary>
        private readonly double[] SUR_n = new double[8760];

        /// <summary>
        ///     Hour deficit todo What's the equation ?
        /// </summary>
        private readonly double[] DEF_n = new double[8760];

        /// <summary>
        ///     Hourly purchased grid electricity
        /// </summary>
        private readonly double[] ELEC_proj = new double[8760];

        /// <summary>
        ///     Hourly purchased natural gas
        /// </summary>
        private readonly double[] NGAS_proj = new double[8760];

        #endregion

        #region AvailableSettings

        private double CCOP_ECH { get; } = PlantSettingsViewModel.Instance.CCOP_ECH;
        private double EFF_NGB { get; } = PlantSettingsViewModel.Instance.EFF_NGB;
        private static double OFF_ABS { get; } = PlantSettingsViewModel.Instance.OFF_ABS;
        private double CCOP_ABS { get; } = PlantSettingsViewModel.Instance.CCOP_ABS;
        private double AUT_BAT { get; } = PlantSettingsViewModel.Instance.AUT_BAT;
        private double LOSS_BAT { get; } = PlantSettingsViewModel.Instance.LOSS_BAT;
        private string TMOD_CHP { get; } = PlantSettingsViewModel.Instance.TMOD_CHP.ToString();
        private double OFF_CHP { get; } = PlantSettingsViewModel.Instance.OFF_CHP;
        private double EFF_CHP { get; } = PlantSettingsViewModel.Instance.EFF_CHP;
        private double HREC_CHP { get; } = PlantSettingsViewModel.Instance.HREC_CHP;
        private static double OFF_EHP { get; } = PlantSettingsViewModel.Instance.OFF_EHP;
        private double HCOP_EHP { get; } = PlantSettingsViewModel.Instance.HCOP_EHP;
        private double AUT_HWT { get; } = PlantSettingsViewModel.Instance.AUT_HWT;
        private double LOSS_HWT { get; } = PlantSettingsViewModel.Instance.LOSS_HWT;
        private double OFF_PV { get; } = PlantSettingsViewModel.Instance.OFF_PV;
        private double UTIL_PV { get; } = PlantSettingsViewModel.Instance.UTIL_PV;
        private double LOSS_PV { get; } = PlantSettingsViewModel.Instance.LOSS_PV;
        private double EFF_PV { get; } = PlantSettingsViewModel.Instance.EFF_PV;
        private double EFF_SHW { get; } = PlantSettingsViewModel.Instance.EFF_SHW;
        private double LOSS_SHW { get; } = PlantSettingsViewModel.Instance.LOSS_SHW;
        private double OFF_SHW { get; } = PlantSettingsViewModel.Instance.OFF_SHW;
        private double UTIL_SHW { get; } = PlantSettingsViewModel.Instance.UTIL_SHW;
        private double CIN_WND { get; } = PlantSettingsViewModel.Instance.CIN_WND;
        private double COP_WND { get; } = PlantSettingsViewModel.Instance.COP_WND;
        private double COUT_WND { get; } = PlantSettingsViewModel.Instance.COUT_WND;
        private double OFF_WND { get; } = PlantSettingsViewModel.Instance.OFF_WND;
        private double ROT_WND { get; } = PlantSettingsViewModel.Instance.ROT_WND;
        private double LOSS_WND { get; } = PlantSettingsViewModel.Instance.LOSS_WND;

        #endregion
    }
}