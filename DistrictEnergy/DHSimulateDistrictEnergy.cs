using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DistrictEnergy.ViewModels;
using EnergyPlusWeather;
using Mit.Umi.Core;
using Mit.Umi.RhinoServices.Context;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

// ReSharper disable ArrangeAccessorOwnerBody

namespace DistrictEnergy
{
    [Guid("929185AA-DB2C-4AA5-B1C0-E89C93F0704D")]
    public class DHSimulateDistrictEnergy : Command
    {
        // ReSharper disable once ArrangeTypeMemberModifiers
        static DHSimulateDistrictEnergy _instance;

        private int _progressBarPos;

        public DHSimulateDistrictEnergy()
        {
            _instance = this;
        }

        ///<summary>The only instance of the DHSimulateDistrictEnergy command.</summary>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public static DHSimulateDistrictEnergy Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DHSimulateDistrictEnergy"; }
        }

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
        private static double[] ELEC_n { get; set; }

        /// <summary>
        ///     Hourly location solar radiation data (kWh/m2)
        /// </summary>
        private static decimal[] RAD_n { get; set; }

        /// <summary>
        ///     Hourly location wind speed data (m/s)
        /// </summary>
        private static decimal[] WIND_n { get; set; }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var umiContext = UmiContext.Current;
            if (umiContext == null)
            {
                RhinoApp.WriteLine("Problem getting the umi context");
                return Result.Failure;
            }

            _progressBarPos = 0;
            // Getting the Aggregated Load Curve for all buildings
            var contextBuildings =
                umiContext.GetObjects().Where(o => o.Data["SDL/Cooling"].Data.Count == 8760).ToList();
            CHW_n = GetHourlyChilledWaterProfile(contextBuildings);
            HW_n = GetHourlyHotWaterLoadProfile(contextBuildings);
            ELEC_n = GetHourlyElectricalLoadProfile(contextBuildings);
            RAD_n = GetHourlyLocationSolarRadiation(umiContext).ToArray();
            WIND_n = GetHourlyLocationWind(umiContext).ToArray();
            StatusBar.HideProgressMeter();

            RhinoApp.WriteLine(
                $"Calculated...\n{CHW_n.Length} datapoints for ColdWater profile\n{HW_n.Count()} datapoints for HotWater\n{ELEC_n.Count()} datapoints for Electricity\n{RAD_n.Count()} datapoints for Solar Frad\n{WIND_n.Count()} datapoints for WindSpeed");

            // Go Hour by hour and parse through the simulation routine
            MainSimulation(CHW_n, HW_n, ELEC_n, RAD_n, WIND_n);

            return Result.Success;
        }

        /// <summary>
        ///     This is the routine that goeas though all the eqautions one timestep at a time
        /// </summary>
        /// <param name="chwN"></param>
        /// <param name="hwN"></param>
        /// <param name="elecN"></param>
        /// <param name="radN"></param>
        /// <param name="windN"></param>
        private void MainSimulation(double[] chwN, double[] hwN, double[] elecN, decimal[] radN, decimal[] windN)
        {
            StatusBar.ShowProgressMeter(0, chwN.Length, "Solving Thermal Plant Components", true, true);
            for (; i < chwN.Length; i++)
            {
                eqHW_ABS(CHW_n[i], out HW_ABS[i]); //OK
                eqELEC_ECH(CHW_n[i], out ELEC_ECH[i]); //OK
                eqELEC_EHP(HW_n[i], out ELEC_EHP[i], out HW_EHP[i]); // OK
                eqHW_SHW((double) RAD_n[i], HW_n[i], HW_EHP[i], out HW_SHW[i], out SHW_BAL[i]); // OK
                eqELEC_PV((double) RAD_n[i], out ELEC_PV[i]); // OK
                eqELEC_WND((double) WIND_n[i], out ELEC_WND[i]); // OK
                if (i == 0)
                    TANK_CHG_n[i] = CAP_HWT * TANK_START;
                if (i > 0)
                    eqTANK_CHG_n(TANK_CHG_n[i - 1], SHW_BAL[i], out TANK_CHG_n[i]); // OK
                eqHW_HWT(TANK_CHG_n[i], out HW_HWT[i]); // OK             

                eqELEC_REN(ELEC_PV[i], ELEC_WND[i], ELEC_n[i], out ELEC_REN[i], out ELEC_BAL[i]); // OK
                if (i == 0)
                    BAT_CHG_n[0] = CAP_BAT * BAT_START;
                if (i > 0)
                    eqBAT_CHG_n(BAT_CHG_n[i - 1], ELEC_BAL[i], out BAT_CHG_n[i]); // OK
                eqELEC_BAT(BAT_CHG_n[i], out ELEC_BAT[i]); // OK

                if (string.Equals(TMOD_CHP, "Thermal"))
                {
                    // ignore NgasChp
                    eqHW_CHP(TMOD_CHP, HW_n[i], HW_EHP[i], HW_ABS[i], HW_SHW[i], HW_HWT[i], NGAS_CHP[i], out HW_CHP[i]);
                    // ignore Elec_Chp
                    eqNGAS_CHP(TMOD_CHP, HW_CHP[i], ELEC_CHP[i], out NGAS_CHP[i]);
                    // ignore ElecN, ElecRen, ElecBat
                    eqELEC_CHP(TMOD_CHP, NGAS_CHP[i], ELEC_n[i], ELEC_REN[i], ELEC_BAT[i], out ELEC_CHP[i]);
                }

                if (string.Equals(TMOD_CHP, "Electrical"))
                {
                    // ignore NgasChp
                    eqELEC_CHP(TMOD_CHP, NGAS_CHP[i], ELEC_n[i], ELEC_REN[i], ELEC_BAT[i], out ELEC_CHP[i]);
                    // ignore HwChp
                    eqNGAS_CHP(TMOD_CHP, HW_CHP[i], ELEC_CHP[i], out NGAS_CHP[i]);
                    // ignore HwN, HwEhp, HwAbs, HwShw, HwHwt
                    eqHW_CHP(TMOD_CHP, HW_n[i], HW_EHP[i], HW_ABS[i], HW_SHW[i], HW_HWT[i], NGAS_CHP[i], out HW_CHP[i]);
                }

                eqNGAS_NGB(HW_n[i], HW_EHP[i], HW_ABS[i], HW_SHW[i], HW_HWT[i], HW_CHP[i], out NGAS_NGB[i],
                    out HW_NGB[i]);
                StatusBar.UpdateProgressMeter(i, true);
            }

            RhinoApp.WriteLine("Distric Energy Simulation complete");
            StatusBar.HideProgressMeter();
        }

        private double[] GetHourlyChilledWaterProfile(List<UmiObject> contextObjects)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating cooling loads");
            var nbDataPoint = 8760;
            var aggregationArray = new double[nbDataPoint];
            StatusBar.HideProgressMeter();
            StatusBar.ShowProgressMeter(0, nbDataPoint * contextObjects.Count * 3,
                "Aggregating Cooling Loads", true, true);

            foreach (var umiObject in contextObjects)
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var d = umiObject.Data["SDL/Cooling"].Data[i];
                    aggregationArray[i] += d;
                    _progressBarPos += 1;
                    StatusBar.UpdateProgressMeter(_progressBarPos, true);
                }

            return aggregationArray;
        }

        private double[] GetHourlyHotWaterLoadProfile(List<UmiObject> contextObjects)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating hot water loads");
            var nbDataPoint = 8760;
            var aggregationArray = new double[nbDataPoint];
            StatusBar.HideProgressMeter();
            StatusBar.ShowProgressMeter(0, nbDataPoint * contextObjects.Count * 3,
                "Aggregating Hot Water Loads", true, true);
            foreach (var umiObject in contextObjects)
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var d = umiObject.Data["SDL/Heating"].Data[i] + umiObject.Data["SDL/Domestic Hot Water"].Data[i];
                    aggregationArray[i] += d;
                    _progressBarPos += 1;
                    StatusBar.UpdateProgressMeter(_progressBarPos, true);
                }

            return aggregationArray;
        }

        private double[] GetHourlyElectricalLoadProfile(List<UmiObject> contextObjects)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating electrical loads: SDL/Equipment + SDL/Lighting");
            var nbDataPoint = 8760;
            var aggreagationArray = new double[nbDataPoint];
            StatusBar.HideProgressMeter();
            StatusBar.ShowProgressMeter(0, nbDataPoint * contextObjects.Count * 3,
                "Aggregating Electrical Loads", true, true);
            foreach (var umiObject in contextObjects)
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var d = umiObject.Data["SDL/Equipment"].Data[i] + umiObject.Data["SDL/Lighting"].Data[i];
                    aggreagationArray[i] += d;
                    _progressBarPos += 1;
                    StatusBar.UpdateProgressMeter(_progressBarPos, true);
                }

            return aggreagationArray;
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
        private static double CAP_ABS
        {
            get { return CHW_n.Max() * OFF_ABS; }
        }

        /// <summary>
        ///     Capacity of Electrical Heat Pumps
        /// </summary>
        private static double CAP_EHP
        {
            get { return HW_n.Max() * OFF_EHP; }
        }

        /// <summary>
        ///     Capacity of Battery, defined as the everage demand times the desired autonomy
        /// </summary>
        private static double CAP_BAT
        {
            get { return ELEC_n.Average() * AUT_BAT; }
        }

        /// <summary>
        ///     Capacity of Hot Water Tank, defined as the everage demand times the desired autonomy
        /// </summary>
        private static double CAP_HWT
        {
            get { return HW_n.Average() * AUT_HWT; }
        }

        /// <summary>
        ///     Capacity of CHP plant
        /// </summary>
        private static double CAP_CHP
        {
            get { return ELEC_n.Sum(); }
        }

        /// <summary>
        ///     Calculated required area of solar thermal collector
        /// </summary>
        private static double AREA_SHW
        {
            get { return HW_n.Sum() * OFF_SHW / ((double) RAD_n.Sum() * EFF_SHW * LOSS_SHW * UTIL_SHW); }
        }

        /// <summary>
        ///     Calculated required area of PV collectors
        /// </summary>
        private static double AREA_PV
        {
            get { return ELEC_n.Sum() * OFF_PV / ((double) RAD_n.Sum() * EFF_PV * LOSS_PV * UTIL_PV); }
        }

        /// <summary>
        ///     Number of turbines needed: Annual electricity needed divided by how much one turbine generates.
        ///     [Annual Energy that needs to be generated/(0.635 x Rotor Area X sum of cubes of all wind speeds within cut-in and
        ///     cut-out speeds x COP)]
        /// </summary>
        private static double NUM_WND
        {
            get
            {
                return ELEC_n.Sum() * OFF_WND / (WIND_n.Where(w => (double) w > CIN_WND && (double) w < COUT_WND)
                                                     .Select(w => Math.Pow((double) w, 3)).Sum() * COP_WND);
            }
        }

        /// <summary>
        ///     Dischrage rate of thermal tank
        /// </summary>
        private static double DCHG_HWT { get; } =
            double.MaxValue; // todo Discharge rate is set to infinity but should be defined in the future

        /// <summary>
        ///     Discharge rate of battery
        /// </summary>
        private static double DCHG_BAT { get; } =
            double.MaxValue; // todo Discharge rate is set to infinity but should be defined in the future

        #endregion

        #region Equation 1 to 18

        /// <summary>
        ///     Equation 1
        /// </summary>
        /// <param name="chwN">Hourly chilled water load profile (kWh)</param>
        /// <param name="hwAbs">Hot water required for Absorption Chiller</param>
        private void eqHW_ABS(double chwN, out double hwAbs)
        {
            hwAbs = Math.Min(chwN, CAP_ABS) / CCOP_ABS;
        }

        /// <summary>
        ///     Any loads in surplus of the absorption chiller capacity are met by electric chillers for each hour. The results
        ///     from this component are hourly profiles for electricity consumption, (ELEC_ECH), to generate project chilled water,
        ///     and the annual total can be expressed as:
        /// </summary>
        /// <param name="chwN">Hourly chilled water load profile (kWh)</param>
        /// <param name="elecEch">Electricity Consumption to generate chilled water from chillers</param>
        private void eqELEC_ECH(double chwN, out double elecEch)
        {
            if (chwN > CAP_ABS) elecEch = (chwN - CAP_ABS) / CCOP_ECH;
            elecEch = 0;
        }

        /// <summary>
        ///     Equation 3 : The electricity consumption required to generate hot water from heat pumps
        /// </summary>
        /// <param name="hwN"></param>
        /// <param name="elecEhp"></param>
        /// <param name="hwEhp"></param>
        private void eqELEC_EHP(double hwN, out double elecEhp, out double hwEhp)
        {
            elecEhp = Math.Min(hwN, CAP_EHP) / HCOP_EHP;
            hwEhp = Math.Min(hwN, CAP_EHP);
        }

        /// <summary>
        ///     Equation 4 : The annual boiler natural gas consumption to generate project hot water
        /// </summary>
        /// <param name="hwN"></param>
        /// <param name="hwEhp"></param>
        /// <param name="hwAbs"></param>
        /// <param name="hwShw"></param>
        /// <param name="hwHwt"></param>
        /// <param name="hwChp"></param>
        /// <param name="ngasNgb"></param>
        /// <param name="hwNgb"></param>
        private void eqNGAS_NGB(double hwN, double hwEhp, double hwAbs, double hwShw, double hwHwt, double hwChp,
            out double ngasNgb, out double hwNgb)
        {
            ngasNgb = (hwN - hwEhp + hwAbs - hwShw - hwHwt - hwChp) / EFF_NGB;
            hwNgb = hwN - hwEhp + hwAbs - hwShw - hwHwt - hwChp;
        }

        /// <summary>
        ///     Equation 5 : The Solar hot water generation that meets part/all of the hot water load
        /// </summary>
        /// <param name="radN">Hourly location solar radiation data (kWh/m2)</param>
        /// <param name="hwN">Hourly hot water load profile (kWh)</param>
        /// <param name="hwEhp">hot water load met by electric heat pumps</param>
        /// <param name="hwShw">hot water load met by solar thermal collectors</param>
        /// <param name="solarBalance">If + goes to tank, If - comes from tank</param>
        private void eqHW_SHW(double radN, double hwN, double hwEhp, out double hwShw, out double solarBalance)
        {
            hwShw = Math.Min(radN * AREA_SHW * EFF_SHW * UTIL_SHW * LOSS_SHW, hwN - hwEhp);
            solarBalance = radN * AREA_SHW * EFF_SHW * UTIL_SHW * LOSS_SHW - hwN + hwEhp;
        }

        /// <summary>
        ///     Equation 6 : The tank charge for each hour
        /// </summary>
        /// <param name="previousTankChgN"></param>
        /// <param name="shwBal"></param>
        /// <param name="tankChgN"></param>
        private void eqTANK_CHG_n(double previousTankChgN, double shwBal, out double tankChgN)
        {
            tankChgN = GetSmallestNonNegative(previousTankChgN + shwBal, CAP_HWT);
        }

        /// <summary>
        ///     Gets the smallest non-negative of two variables
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static double GetSmallestNonNegative(double a, double b)
        {
            if (a >= 0 && b >= 0)
                return Math.Min(a, b);
            if (a >= 0 && b < 0)
                return a;
            if (a < 0 && b >= 0)
                return b;
            return 0;
        }

        /// <summary>
        ///     Equation 7 : Demand met by hot water tanks
        /// </summary>
        /// <param name="tankChgN"></param>
        /// <param name="hwHwt"></param>
        private void eqHW_HWT(double tankChgN, out double hwHwt)
        {
            hwHwt = Math.Min(tankChgN, DCHG_HWT);
        }

        /// <summary>
        ///     Equation 8 : The PV total electricity generation
        /// </summary>
        /// <param name="radN"></param>
        /// <param name="elecPv"></param>
        private void eqELEC_PV(double radN, out double elecPv)
        {
            elecPv = radN * AREA_PV * EFF_PV * UTIL_PV * LOSS_PV;
        }

        /// <summary>
        ///     Equation 9 : The annual electricity generation for Wind Turbines
        /// </summary>
        /// <param name="windN"></param>
        /// <param name="elecWnd"></param>
        private void eqELEC_WND(double windN, out double elecWnd)
        {
            elecWnd = 0.6375 * Math.Pow(windN, 3) * ROT_WND * NUM_WND * COP_WND * LOSS_WND;
        }

        /// <summary>
        ///     Equation 10 : The total renewable electricity
        /// </summary>
        /// <param name="elecPv"></param>
        /// <param name="elecWnd"></param>
        /// <param name="elecN"></param>
        /// <param name="elecRen"></param>
        /// <param name="elecBalance"></param>
        private void eqELEC_REN(double elecPv, double elecWnd, double elecN, out double elecRen, out double elecBalance)
        {
            elecRen = Math.Min(elecPv + elecWnd, elecN);
            elecBalance = elecPv + elecWnd - elecN;
        }

        /// <summary>
        ///     Equation 11 : The battery charge for each hour
        /// </summary>
        /// <param name="previousBatChgN"></param>
        /// <param name="elecBalance"></param>
        /// <param name="batChgN"></param>
        private void eqBAT_CHG_n(double previousBatChgN,
            double elecBalance, out double batChgN)
        {
            batChgN = GetSmallestNonNegative(previousBatChgN + elecBalance, CAP_BAT);
        }

        /// <summary>
        ///     Equation 12 : Demand met by the battery bank
        /// </summary>
        /// <param name="batChgN"></param>
        /// <param name="elecBat"></param>
        private void eqELEC_BAT(double batChgN, out double elecBat)
        {
            elecBat = Math.Min(batChgN, DCHG_BAT);
        }

        /// <summary>
        ///     Equation  13/18 : The annual heating energy recovered from the combined heat and power plant and supplied to the
        ///     project
        /// </summary>
        private void eqHW_CHP(string tracking, double hWn, double hwEhp, double hwAbs, double hwShw, double hwHwt,
            double ngasChp,
            out double hwChp)
        {
            if (string.Equals(tracking, "Thermal"))
                hwChp = Math.Min(CAP_CHP / EFF_CHP * HREC_CHP,
                    hWn - hwEhp + hwAbs - hwShw - hwHwt);
            hwChp = ngasChp * HREC_CHP;
        }

        /// <summary>
        ///     Equation 14/17 : The natural gas consumed by the CHP plant
        /// </summary>
        /// <param name="tracking">The tracking mode of the CHP plant (converted to a string : eg "Thermal" of "Electrical")</param>
        /// <param name="hwChp"></param>
        /// <param name="elecChp"></param>
        /// <param name="ngasChp"></param>
        private void eqNGAS_CHP(string tracking, double hwChp, double elecChp, out double ngasChp)
        {
            if (string.Equals(tracking, "Thermal"))
                ngasChp = hwChp / HREC_CHP;
            ngasChp = elecChp / EFF_CHP;
        }

        /// <summary>
        ///     Equation 15/16 : The the electricity generated by the CHP plant
        /// </summary>
        /// <param name="tracking"></param>
        /// <param name="ngasChp"></param>
        /// <param name="elecN"></param>
        /// <param name="elecRen"></param>
        /// <param name="elecBat"></param>
        /// <param name="elecChp"></param>
        private void eqELEC_CHP(string tracking, double ngasChp, double elecN, double elecRen, double elecBat,
            out double elecChp)
        {
            if (string.Equals(tracking, "Thermal"))
                elecChp = ngasChp * EFF_CHP;
            elecChp = Math.Min(CAP_CHP, elecN - elecRen - elecBat);
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
        ///     Electricity generation balance from renewables only
        /// </summary>
        private readonly double[] ELEC_BAL = new double[8760];

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
        ///     Hot Water produced by Natural Gas Boilers
        /// </summary>
        private readonly double[] HW_NGB = new double[8760];

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
        ///     Hour Surplus
        /// </summary>
        private readonly double[] SHW_BAL = new double[8760];

        /// <summary>
        ///     Hourly purchased grid electricity
        /// </summary>
        private readonly double[] ELEC_proj = new double[8760];

        /// <summary>
        ///     Hourly purchased natural gas
        /// </summary>
        private readonly double[] NGAS_proj = new double[8760];

        /// <summary>
        ///     Simulation Timestep
        /// </summary>
        private static int i;

        #endregion

        #region AvailableSettings

        private static double CCOP_ECH
        {
            get { return PlantSettingsViewModel.Instance.CCOP_ECH; }
        }

        private static double EFF_NGB
        {
            get { return PlantSettingsViewModel.Instance.EFF_NGB; }
        }

        private static double OFF_ABS
        {
            get { return PlantSettingsViewModel.Instance.OFF_ABS; }
        }

        private static double CCOP_ABS
        {
            get { return PlantSettingsViewModel.Instance.CCOP_ABS; }
        }

        private static double AUT_BAT
        {
            get { return PlantSettingsViewModel.Instance.AUT_BAT; }
        }

        private static double LOSS_BAT
        {
            get { return PlantSettingsViewModel.Instance.LOSS_BAT; }
        }

        private static string TMOD_CHP
        {
            get { return PlantSettingsViewModel.Instance.TMOD_CHP.ToString(); }
        }

        private static double OFF_CHP
        {
            get { return PlantSettingsViewModel.Instance.OFF_CHP; }
        }

        private static double EFF_CHP
        {
            get { return PlantSettingsViewModel.Instance.EFF_CHP; }
        }

        private static double HREC_CHP
        {
            get { return PlantSettingsViewModel.Instance.HREC_CHP; }
        }

        private static double OFF_EHP
        {
            get { return PlantSettingsViewModel.Instance.OFF_EHP; }
        }

        private static double HCOP_EHP
        {
            get { return PlantSettingsViewModel.Instance.HCOP_EHP; }
        }

        private static double AUT_HWT
        {
            get { return PlantSettingsViewModel.Instance.AUT_HWT; }
        }

        private static double LOSS_HWT
        {
            get { return PlantSettingsViewModel.Instance.LOSS_HWT; }
        }

        private static double OFF_PV
        {
            get { return PlantSettingsViewModel.Instance.OFF_PV; }
        }

        private static double UTIL_PV
        {
            get { return PlantSettingsViewModel.Instance.UTIL_PV; }
        }

        private static double LOSS_PV
        {
            get { return PlantSettingsViewModel.Instance.LOSS_PV; }
        }

        private static double EFF_PV
        {
            get { return PlantSettingsViewModel.Instance.EFF_PV; }
        }

        /// <summary>
        ///     Collector efficiency (%)
        /// </summary>
        private static double EFF_SHW
        {
            get { return PlantSettingsViewModel.Instance.EFF_SHW; }
        }

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        private static double LOSS_SHW
        {
            get { return PlantSettingsViewModel.Instance.LOSS_SHW; }
        }

        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        private static double OFF_SHW
        {
            get { return PlantSettingsViewModel.Instance.OFF_SHW; }
        }

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        private static double UTIL_SHW
        {
            get { return PlantSettingsViewModel.Instance.UTIL_SHW; }
        }

        private static double CIN_WND
        {
            get { return PlantSettingsViewModel.Instance.CIN_WND; }
        }

        private static double COP_WND
        {
            get { return PlantSettingsViewModel.Instance.COP_WND; }
        }

        private static double COUT_WND
        {
            get { return PlantSettingsViewModel.Instance.COUT_WND; }
        }

        private static double OFF_WND
        {
            get { return PlantSettingsViewModel.Instance.OFF_WND; }
        }

        private static double ROT_WND
        {
            get { return PlantSettingsViewModel.Instance.ROT_WND; }
        }

        private static double LOSS_WND
        {
            get { return PlantSettingsViewModel.Instance.LOSS_WND; }
        }

        /// <summary>
        ///     The Tank charged state at the begining of the simulation
        /// </summary>
        private static double TANK_START
        {
            get { return PlantSettingsViewModel.Instance.TANK_START; }
        }

        /// <summary>
        ///     The Battery charged state at the begining of the simulation
        /// </summary>
        private static double BAT_START
        {
            get { return PlantSettingsViewModel.Instance.BAT_START; }
        }

        private static double LOSS_HWNET
        {
            get
            {
                return 0;
            } // todo Should a Hot Water network loss be added as a User Parameter (Default 15%) Add new user value
        }

        private static double LOSS_CHWNET
        {
            get
            {
                return 0;
            } // todo Should a Chilled Water network loss be added as a User Parameter (Default 5%) Add new user value
        }

        #endregion
    }
}