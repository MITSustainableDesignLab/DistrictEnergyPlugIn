using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CsvHelper;
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

        /// <summary>
        ///     Simulation Timestep
        /// </summary>
        private static int i;

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
        ///     Hourly Global Solar Radiation from EPW file (kWh/m2)
        /// </summary>
        private static double[] RAD_n { get; set; }

        /// <summary>
        ///     Hourly location wind speed data (m/s)
        /// </summary>
        private static double[] WIND_n { get; set; }
        /// <summary>
        ///     Hourly Ambiant temperature (C)
        /// </summary>
        private static double[] T_AMB_n { get; set; }

        /// <summary>
        ///     This is the routine that goeas though all the eqautions one timestep at a time
        /// </summary>
        private void MainSimulation()
        {
            i = 0;
            StatusBar.ShowProgressMeter(0, numberTimesteps, "Solving Thermal Plant Components", true, true);
            for (; i < numberTimesteps; i++)
            {
                if (i == 3878)
                    Debugger.Break();
                eqHW_ABS(CHW_n[i], out HW_ABS[i], out CHW_ABS[i]); //OK
                eqELEC_ECH(CHW_n[i], out ELEC_ECH[i], out CHW_ECH[i]); //OK
                eqHW_SHW(RAD_n[i], HW_n[i], HW_ABS[i], out HW_SHW[i], out SHW_BAL[i]); // OK todo Surplus energy from the chp should go in the battery
                eqELEC_PV(RAD_n[i], out ELEC_PV[i]); // OK
                eqELEC_WND(WIND_n[i], out ELEC_WND[i]); // OK
                if (i == 0)
                    TANK_CHG_n[i] = CAP_HWT * TANK_START;
                if (i > 0)
                    eqTANK_CHG_n(TANK_CHG_n[i - 1], SHW_BAL[i], T_AMB_n[i], out TANK_CHG_n[i]); // OK
                eqHW_HWT(TANK_CHG_n[i], HW_n[i], HW_ABS[i], HW_SHW[i], out HW_HWT[i]); // OK
                eqELEC_EHP(HW_n[i], HW_ABS[i], HW_SHW[i], HW_HWT[i], HW_CHP[i], out ELEC_EHP[i],
                    out HW_EHP[i]); // Will call HW_CHP even though its not calculated yet


                eqELEC_REN(ELEC_PV[i], ELEC_WND[i], ELEC_n[i], ELEC_ECH[i], ELEC_EHP[i], out ELEC_REN[i], out ELEC_BAL[i]); // OK
                if (i == 0)
                    BAT_CHG_n[0] = CAP_BAT * BAT_START;
                if (i > 0)
                    eqBAT_CHG_n(BAT_CHG_n[i - 1], ELEC_BAL[i], out BAT_CHG_n[i]); // OK
                eqELEC_BAT(BAT_CHG_n[i], ELEC_n[i], ELEC_ECH[i], ELEC_EHP[i], out ELEC_BAT[i], ELEC_REN[i], ELEC_CHP[i]); // OK

                if (string.Equals(TMOD_CHP, "Thermal"))
                {
                    // ignore NgasChp
                    eqHW_CHP(TMOD_CHP, HW_n[i], HW_EHP[i], HW_ABS[i], HW_SHW[i], HW_HWT[i], NGAS_CHP[i], out HW_CHP[i]);
                    // ignore Elec_Chp
                    eqNGAS_CHP(TMOD_CHP, HW_CHP[i], ELEC_CHP[i], out NGAS_CHP[i]);
                    // ignore ElecN, ElecRen, ElecBat
                    eqELEC_CHP(TMOD_CHP, NGAS_CHP[i], ELEC_n[i], ELEC_REN[i], ELEC_BAT[i], ELEC_ECH[i], ELEC_EHP[i],
                        out ELEC_CHP[i]);
                }

                if (string.Equals(TMOD_CHP, "Electrical"))
                {
                    // ignore NgasChp
                    eqELEC_CHP(TMOD_CHP, NGAS_CHP[i], ELEC_n[i], ELEC_REN[i], ELEC_BAT[i], ELEC_ECH[i], ELEC_EHP[i],
                        out ELEC_CHP[i]);
                    // ignore HwChp
                    eqNGAS_CHP(TMOD_CHP, HW_CHP[i], ELEC_CHP[i], out NGAS_CHP[i]);
                    // ignore HwN, HwEhp, HwAbs, HwShw, HwHwt
                    eqHW_CHP(TMOD_CHP, HW_n[i], HW_EHP[i], HW_ABS[i], HW_SHW[i], HW_HWT[i], NGAS_CHP[i], out HW_CHP[i]);
                }

                eqNGAS_NGB(HW_n[i], HW_EHP[i], HW_ABS[i], HW_SHW[i], HW_HWT[i], HW_CHP[i], out NGAS_NGB[i],
                    out HW_NGB[i]);
                eqELEC_proj(ELEC_n[i], ELEC_REN[i], ELEC_BAT[i], ELEC_CHP[i], ELEC_EHP[i], ELEC_ECH[i], out ELEC_PROJ[i]);
                eqNGAS_proj(NGAS_NGB[i], NGAS_CHP[i], out NGAS_PROJ[i]);
                StatusBar.UpdateProgressMeter(i, true);
            }

            RhinoApp.WriteLine("Distric Energy Simulation complete");
            StatusBar.HideProgressMeter();
        }

        private int numberTimesteps { get; set; }

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
            StatusBar.HideProgressMeter();
            RAD_n = GetHourlyLocationSolarRadiation(umiContext).ToArray();
            WIND_n = GetHourlyLocationWind(umiContext).ToArray();
            T_AMB_n = GetHourlyLocationAmbiantTemp(umiContext).ToArray();
            

            numberTimesteps = HW_n.Length;

            RhinoApp.WriteLine(
                $"Calculated...\n{CHW_n.Length} datapoints for ColdWater profile\n{HW_n.Count()} datapoints for HotWater\n{ELEC_n.Count()} datapoints for Electricity\n{RAD_n.Count()} datapoints for Solar Frad\n{WIND_n.Count()} datapoints for WindSpeed");

            // Go Hour by hour and parse through the simulation routine
            SetResultsArraystoZero();
            DeleteLogFile();
            CalculateConstants();
            MainSimulation();
            SimulationResultsToCsv();

            return Result.Success;
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

        private IEnumerable<double> GetHourlyLocationSolarRadiation(UmiContext context)
        {
            RhinoApp.WriteLine("Calculating Solar Radiation on horizontal surface");
            var a = new EPWeatherData();
            a.GetRawData(context.WeatherFilePath);
            return a.HourlyWeatherDataRawList.Select(b => (double) b.GHorRadiation / 1000.0); // from Wh to kWh
        }

        private IEnumerable<double> GetHourlyLocationWind(UmiContext context)
        {
            RhinoApp.WriteLine("Calculating wind for location");
            var a = new EPWeatherData();
            a.GetRawData(context.WeatherFilePath);
            return a.HourlyWeatherDataRawList.Select(b => (double) b.WindSpeed); // m/s
        }

        private IEnumerable<double> GetHourlyLocationAmbiantTemp(UmiContext context)
        {
            RhinoApp.WriteLine("Calculating temp for location");
            var a = new EPWeatherData();
            a.GetRawData(context.WeatherFilePath);
            return a.HourlyWeatherDataRawList.Select(b => (double)b.DB); // C
        }

        /// <summary>
        ///     Equation 19 : Hourly purchased grid Wlectricity for whole site
        /// </summary>
        /// <param name="elecN"></param>
        /// <param name="elecRen"></param>
        /// <param name="elecBat"></param>
        /// <param name="elecChp"></param>
        /// <param name="elecEhp"></param>
        /// <param name="elecEch"></param>
        /// <param name="elecProj"></param>
        private void eqELEC_proj(double elecN, double elecRen, double elecBat, double elecChp,
            double elecEhp, double elecEch, out double elecProj)
        {
            elecProj = elecN - elecRen - elecBat - elecChp + elecEhp + elecEch;
        }

        /// <summary>
        ///     Equation 20 : Hourly purchased Natural Gas for whole site
        /// </summary>
        /// <param name="ngasNgb"></param>
        /// <param name="ngasChp"></param>
        /// <param name="ngasProj"></param>
        private void eqNGAS_proj(double ngasNgb, double ngasChp, out double ngasProj)
        {
            ngasProj = ngasNgb + ngasChp;
        }

        private void SimulationResultsToCsv()
        {
            var file_name = @"C:\UMI\temp\DHSimulationResults.csv";
            using (var writer = new StreamWriter(file_name))
            using (var csvWriter = new CsvWriter(writer))
            {
                var Headers = new List<string>();
                Headers.Add("DateTime");    // 1
                Headers.Add("Hour");        // 2
                Headers.Add("BAT_CHG_n");   // 3
                Headers.Add("ELEC_BAT");    // 4
                Headers.Add("ELEC_CHP");    // 5
                Headers.Add("ELEC_ECH");    // 6
                Headers.Add("ELEC_EHP");    // 7
                Headers.Add("ELEC_PV");     // 8
                Headers.Add("ELEC_REN");    // 9
                Headers.Add("ELEC_WND");    // 10
                Headers.Add("ELEC_BAL");    // 11
                Headers.Add("HW_ABS");      // 12
                Headers.Add("HW_EHP");      // 13
                Headers.Add("HW_CHP");      // 14
                Headers.Add("HW_HWT");      // 15
                Headers.Add("HW_SHW");      // 16
                Headers.Add("HW_NGB");      // 17
                Headers.Add("NGAS_CHP");    // 18
                Headers.Add("NGAS_NGB");    // 19
                Headers.Add("TANK_CHG_n");  // 20
                Headers.Add("SHW_BAL");     // 21
                Headers.Add("ELEC_PROJ");   // 22
                Headers.Add("NGAS_PROJ");   // 23
                Headers.Add("CHW_n");       // 24
                Headers.Add("HW_n");        // 25
                Headers.Add("ELEC_n");      // 26
                Headers.Add("CHW_ABS");     // 27
                Headers.Add("CHW_ECH");     // 28

                foreach (var header in Headers) csvWriter.WriteField(header);

                csvWriter.NextRecord();

                StatusBar.HideProgressMeter();
                StatusBar.ShowProgressMeter(0, 8760, "Saving Results to CSV", true, true);
                var dateTime = new DateTime(2017, 1, 1, 0, 0, 0);

                for (var i = 0; i < 8760; i++)
                {
                    csvWriter.WriteField(dateTime);         // 1
                    csvWriter.WriteField(i);                // 2
                    csvWriter.WriteField(BAT_CHG_n[i]);     // 3
                    csvWriter.WriteField(ELEC_BAT[i]);      // 4
                    csvWriter.WriteField(ELEC_CHP[i]);      // 5
                    csvWriter.WriteField(ELEC_ECH[i]);      // 6
                    csvWriter.WriteField(ELEC_EHP[i]);      // 7
                    csvWriter.WriteField(ELEC_PV[i]);       // 8
                    csvWriter.WriteField(ELEC_REN[i]);      // 9
                    csvWriter.WriteField(ELEC_WND[i]);      // 10
                    csvWriter.WriteField(ELEC_BAL[i]);      // 11
                    csvWriter.WriteField(HW_ABS[i]);        // 12
                    csvWriter.WriteField(HW_EHP[i]);        // 13
                    csvWriter.WriteField(HW_CHP[i]);        // 14
                    csvWriter.WriteField(HW_HWT[i]);        // 15
                    csvWriter.WriteField(HW_SHW[i]);        // 16
                    csvWriter.WriteField(HW_NGB[i]);        // 17
                    csvWriter.WriteField(NGAS_CHP[i]);      // 18
                    csvWriter.WriteField(NGAS_NGB[i]);      // 19
                    csvWriter.WriteField(TANK_CHG_n[i]);    // 20
                    csvWriter.WriteField(SHW_BAL[i]);       // 21
                    csvWriter.WriteField(ELEC_PROJ[i]);     // 22
                    csvWriter.WriteField(NGAS_PROJ[i]);     // 23
                    csvWriter.WriteField(CHW_n[i]);         // 24
                    csvWriter.WriteField(HW_n[i]);          // 25
                    csvWriter.WriteField(ELEC_n[i]);        // 26
                    csvWriter.WriteField(CHW_ABS[i]);       // 27
                    csvWriter.WriteField(CHW_ECH[i]);       // 28

                    csvWriter.NextRecord();
                    dateTime = dateTime.AddHours(1);
                    StatusBar.UpdateProgressMeter(i, true);
                }

                StatusBar.HideProgressMeter();
                writer.Close();
            }

            RhinoApp.WriteLine(string.Format("CSV file successfully written to {}", file_name));
        }


        public string GetUmiTempPath()
        {
            var path = @"C:\UMI\temp";
            if (!path.EndsWith("\\")) path += "\\";
            return path;
        }

        public void LogMessageToFile(string msg, int atTimestep)
        {
            var sw = File.AppendText(
                GetUmiTempPath() + "DHSimulationLogFile.txt");
            try
            {
                var logLine = string.Format(
                    "{0:G}: _{1}_{2}.", DateTime.Now, atTimestep, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }

        public void DeleteLogFile()
        {
            File.Delete(GetUmiTempPath() + "DHSimulationLogFile.txt");
        }

        #region Constants

        /// <summary>
        ///     Calculates the necessary constants used in different equations
        /// </summary>
        private void CalculateConstants()
        {
            CAP_ABS = CHW_n.Max() * OFF_ABS;
            CAP_EHP = HW_n.Max() * OFF_EHP;
            CAP_BAT = ELEC_n.Average() * AUT_BAT * 24;
            CAP_HWT = HW_n.Average() * AUT_HWT * 24; // todo Prendre jour moyen du mois max.
            CAP_CHP = ELEC_n.Max() * OFF_CHP;
            AREA_SHW = HW_n.Sum() * OFF_SHW / (RAD_n.Sum() * EFF_SHW * (1 - LOSS_SHW) * UTIL_SHW);
            AREA_PV = ELEC_n.Sum() * OFF_PV / (RAD_n.Sum() * EFF_PV * (1 - LOSS_PV) * UTIL_PV);
            var windCubed = WIND_n.Where(w => w > CIN_WND && w < COUT_WND).Select(w => Math.Pow(w, 3)).Sum();
            NUM_WND = ELEC_n.Sum() * OFF_WND /
                      (0.6375 * windCubed * ROT_WND * (1 - LOSS_WND) * COP_WND /
                       1000); // Divide by 1000 because equation spits out Wh
            CHGR_HWT = CAP_HWT == 0 ? 0 : CAP_HWT / 12; // 12 hours // (AUT_HWT * 12);
            CHGR_BAT = CAP_BAT == 0 ? 0 : CAP_BAT / 12;
            DCHGR_HWT = CAP_HWT == 0
                ? 0
                : CAP_HWT / 12; // (AUT_HWT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
            DCHG_BAT = CAP_BAT == 0
                ? 0
                : CAP_BAT / 12; // (AUT_BAT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
        }

        /// <summary>
        ///     Cooling capacity of absorption chillers (kW)
        /// </summary>
        private static double CAP_ABS { get; set; }

        /// <summary>
        ///     Capacity of Electrical Heat Pumps
        /// </summary>
        private static double CAP_EHP { get; set; }

        /// <summary>
        ///     Capacity of Battery, defined as the everage demand times the desired autonomy
        /// </summary>
        private static double CAP_BAT { get; set; }

        /// <summary>
        ///     Capacity of Hot Water Tank, defined as the everage demand times the desired autonomy
        /// </summary>
        private static double CAP_HWT { get; set; }

        /// <summary>
        ///     Capacity of CHP plant
        /// </summary>
        private static double CAP_CHP { get; set; }

        /// <summary>
        ///     Calculated required area of solar thermal collector (m^2)
        /// </summary>
        private static double AREA_SHW { get; set; }

        /// <summary>
        ///     Calculated required area of PV collectors
        /// </summary>
        private static double AREA_PV { get; set; }

        /// <summary>
        ///     Number of turbines needed: Annual electricity needed divided by how much one turbine generates.
        ///     [Annual Energy that needs to be generated/(0.635 x Rotor Area X sum of cubes of all wind speeds within cut-in and
        ///     cut-out speeds x COP)]
        /// </summary>
        private static double NUM_WND { get; set; }

        /// <summary>
        ///     The Hot Water Tank Charge Rate (kWh / h)
        /// </summary>
        public static double CHGR_HWT { get; set; }

        /// <summary>
        ///     The Battery Charge Rate (kWh / h)
        /// </summary>
        public static double CHGR_BAT { get; set; }

        /// <summary>
        ///     Discharge rate of Hot Water Tank
        /// </summary>
        private static double DCHGR_HWT { get; set; }

        /// <summary>
        ///     Discharge rate of battery
        /// </summary>
        private static double DCHG_BAT { get; set; }
        /// <summary>
        ///  Hot Water Tank Losses (dependant of the outdoor temperature)
        /// </summary>
        private static double LOSS_HWT { get; set; }

        #endregion

        #region Equation 1 to 18

        /// <summary>
        ///     Equation 1 : The hot water required to generate project chilled water
        /// </summary>
        /// <param name="chwN">Hourly chilled water load profile (kWh)</param>
        /// <param name="hwAbs">Hot water required for Absorption Chiller</param>
        /// <param name="chwAbs"></param>
        private void eqHW_ABS(double chwN, out double hwAbs, out double chwAbs)
        {
            hwAbs = Math.Min(chwN, CAP_ABS) / CCOP_ABS;
            chwAbs = Math.Min(chwN, CAP_ABS);
        }

        /// <summary>
        ///     Any loads in surplus of the absorption chiller capacity are met by electric chillers for each hour. The results
        ///     from this component are hourly profiles for electricity consumption, (ELEC_ECH), to generate project chilled water,
        ///     and the annual total can be expressed as:
        /// </summary>
        /// <param name="chwN">Hourly chilled water load profile (kWh)</param>
        /// <param name="elecEch">Electricity Consumption to generate chilled water from chillers</param>
        /// <param name="chwEch"></param>
        private void eqELEC_ECH(double chwN, out double elecEch, out double chwEch)
        {
            if (chwN > CAP_ABS)
            {
                elecEch = (chwN - CAP_ABS) / CCOP_ECH;
                chwEch = chwN - CAP_ABS;
            }
            else
            {
                elecEch = 0;
                chwEch = 0;
            }
        }

        /// <summary>
        ///     Equation 3 : The electricity consumption required to generate hot water from heat pumps
        /// </summary>
        /// <param name="hwN">Hourly hot water load profile (kWh)</param>
        /// <param name="hwAbs"></param>
        /// <param name="hwShw"></param>
        /// <param name="hwHwt"></param>
        /// <param name="hwChp"></param>
        /// <param name="elecEhp"></param>
        /// <param name="hwEhp"></param>
        private void eqELEC_EHP(double hwN, double hwAbs, double hwShw,
            double hwHwt, double hwChp, out double elecEhp, out double hwEhp)
        {
            elecEhp = GetSmallestNonNegative(hwN + hwAbs - hwShw - hwHwt - hwChp, CAP_EHP) / HCOP_EHP;
            hwEhp = GetSmallestNonNegative(hwN + hwAbs - hwShw - hwHwt - hwChp, CAP_EHP);
        }

        /// <summary>
        ///     Equation 4 : The boiler natural gas consumption to generate project hot water
        /// </summary>
        /// <param name="hwN">Hourly hot water load profile (kWh)</param>
        /// <param name="hwEhp">hot water load met by electric heat pumps</param>
        /// <param name="hwAbs">Hot water load needed by the Absorption chiller</param>
        /// <param name="hwShw">hot water load met by solar thermal collectors</param>
        /// <param name="hwHwt">Hot water Demand met by hot water tanks</param>
        /// <param name="hwChp">Hot Water Demand met by CHP plant</param>
        /// <param name="ngasNgb">Natural gas consumption to generate project hot water</param>
        /// <param name="hwNgb">Hot Water produced by Natural Gas Boilers</param>
        private void eqNGAS_NGB(double hwN, double hwEhp, double hwAbs, double hwShw, double hwHwt, double hwChp,
            out double ngasNgb, out double hwNgb)
        {
            ngasNgb = Math.Max(hwN - hwEhp + hwAbs - hwShw - hwHwt - hwChp, 0) / EFF_NGB; // If chp produces more energy than needed, ngas will be 0.
            hwNgb = Math.Max(hwN - hwEhp + hwAbs - hwShw - hwHwt - hwChp, 0);
        }

        /// <summary>
        ///     Equation 5 : The Solar hot water generation that meets part/all of the hot water load
        /// </summary>
        /// <param name="radN">Hourly location solar radiation data (kWh/m2)</param>
        /// <param name="hwN">Hourly hot water load profile (kWh)</param>
        /// <param name="hwAbs">Hot water load needed by the Absorption chiller</param>
        /// <param name="hwShw">hot water load met by solar thermal collectors</param>
        /// <param name="solarBalance">If + goes to tank, If - comes from tank</param>
        private void eqHW_SHW(double radN, double hwN, double hwAbs, out double hwShw,
            out double solarBalance)
        {
            hwShw = Math.Min(radN * AREA_SHW * EFF_SHW * UTIL_SHW * (1 - LOSS_SHW), hwN + hwAbs);
            solarBalance = radN * AREA_SHW * EFF_SHW * UTIL_SHW * (1 - LOSS_SHW) - hwN - hwAbs;
        }

        /// <summary>
        ///     Equation 6 : The tank charge for each hour. Limited by it's charging/discharging rate.
        /// </summary>
        /// <param name="previousTankChgN">Previous timestep Hot Water Tank charge (kWh)</param>
        /// <param name="shwBal">Solar balance</param>
        /// <param name="tAmb"></param>
        /// <param name="tankChgN">This timestep's Hot Water Tank charge (kWh)</param>
        private void eqTANK_CHG_n(double previousTankChgN, double shwBal, double tAmb, out double tankChgN)
        {
            
            if (shwBal < 0) // We are discharging the tank; Loss applies to the newly calculated charge
            {
                tankChgN = Math.Max(previousTankChgN + shwBal, GetHighestNonNegative(previousTankChgN - DCHGR_HWT, 0));
                LOSS_HWT = (-4E-5 * tAmb + 0.0024) * Math.Pow(tankChgN, -1 / 3);
                tankChgN = tankChgN * (1 - LOSS_HWT);
            }
            else if (shwBal > 0) // We are charging the tank
            {
                tankChgN = GetSmallestNonNegative(previousTankChgN + shwBal, GetSmallestNonNegative(previousTankChgN + CHGR_HWT, CAP_HWT));
                LOSS_HWT = (-4E-5 * tAmb + 0.0024) * Math.Pow(tankChgN, -1 / 3);
                tankChgN = tankChgN * (1 - LOSS_HWT);
            }
            else // We are not doing anything, but losses still occur
            {
                tankChgN = previousTankChgN *
                           (1 - LOSS_HWT); // Contrary to the Grasshopper code, the tank loses energy to the environnement even when not used.
            }
        }

        /// <summary>
        ///     Equation 7 : Demand met by hot water tanks
        /// </summary>
        /// <param name="tankChgN"></param>
        /// <param name="hwN"></param>
        /// <param name="hwAbs"></param>
        /// <param name="hwShw"></param>
        /// <param name="hwHwt">Demand met by hot water tank</param>
        private void eqHW_HWT(double tankChgN, double hwN, double hwAbs, double hwShw, out double hwHwt)
        {
            hwHwt = GetSmallestNonNegative(hwN + hwAbs - hwShw, GetSmallestNonNegative(tankChgN, DCHGR_HWT));
        }

        /// <summary>
        ///     Equation 8 : The PV total electricity generation
        /// </summary>
        /// <param name="radN"></param>
        /// <param name="elecPv"></param>
        private void eqELEC_PV(double radN, out double elecPv)
        {
            elecPv = radN * AREA_PV * EFF_PV * UTIL_PV * (1 - LOSS_PV);
        }

        /// <summary>
        ///     Equation 9 : The annual electricity generation for Wind Turbines
        /// </summary>
        /// <param name="windN"></param>
        /// <param name="elecWnd"></param>
        private void eqELEC_WND(double windN, out double elecWnd)
        {
            elecWnd = 0.6375 * Math.Pow(windN, 3) * ROT_WND * NUM_WND * COP_WND * (1 - LOSS_WND) /
                      1000; // Equation spits out Wh
        }

        /// <summary>
        ///     Equation 10 : The total renewable electricity
        /// </summary>
        /// <param name="elecPv"></param>
        /// <param name="elecWnd"></param>
        /// <param name="elecN"></param>
        /// <param name="elecEch"></param>
        /// <param name="elecEhp"></param>
        /// <param name="elecRen"></param>
        /// <param name="elecBalance"></param>
        private void eqELEC_REN(double elecPv, double elecWnd, double elecN,
            double elecEch, double elecEhp, out double elecRen, out double elecBalance)
        {
            elecRen = Math.Min(elecPv + elecWnd, elecN + elecEch + elecEhp);
            elecBalance = elecPv + elecWnd - elecN - elecEch - elecEhp;
        }

        /// <summary>
        ///     Equation 11 : The battery charge for each hour
        /// </summary>
        /// <param name="previousBatChgN">Previous timestep Battery charge (kWh)</param>
        /// <param name="elecBalance">Renewable Energy Balance</param>
        /// <param name="batChgN">This timestep's Battery charge (kWh)</param>
        private void eqBAT_CHG_n(double previousBatChgN,
            double elecBalance, out double batChgN)
        {
            if (elecBalance < 0) // We are discharging the battery; Not all the elecBalance can be fed to the batt because of the LOSS_BAT parameter.
                batChgN = Math.Max(previousBatChgN + elecBalance * (1 - LOSS_BAT), GetHighestNonNegative(previousBatChgN - DCHG_BAT, 0));
            else if (elecBalance > 0) // We are charging the battery
                batChgN = GetSmallestNonNegative(previousBatChgN + elecBalance * (1 - LOSS_BAT), GetSmallestNonNegative(previousBatChgN + CHGR_BAT, CAP_BAT));
            else
                batChgN = previousBatChgN;
        }

        /// <summary>
        ///     Equation 12 : Demand met by the battery bank
        /// </summary>
        /// <param name="batChgN"></param>
        /// <param name="elecN"></param>
        /// <param name="elecEch"></param>
        /// <param name="elecEhp"></param>
        /// <param name="elecBat"></param>
        /// <param name="elecRen"></param>
        /// <param name="elecChp"></param>
        private void eqELEC_BAT(double batChgN, double elecN, double elecEch, double elecEhp, out double elecBat,
            double elecRen, double elecChp)
        {
            elecBat = GetSmallestNonNegative(elecN + elecEch + elecEhp - elecRen - elecChp, GetSmallestNonNegative(batChgN, DCHG_BAT));
        }

        /// <summary>
        ///     Equation  13/18 : The annual heating energy recovered from the combined heat and power plant and supplied to the
        ///     project
        /// </summary>
        /// <param name="tracking">The tracking mode of the CHP plant (converted to a string : eg "Thermal" of "Electrical")</param>
        /// <param name="hWn"></param>
        /// <param name="hwEhp"></param>
        /// <param name="hwAbs"></param>
        /// <param name="hwShw"></param>
        /// <param name="hwHwt"></param>
        /// <param name="ngasChp"></param>
        /// <param name="hwChp"></param>
        private void eqHW_CHP(string tracking, double hWn, double hwEhp, double hwAbs, double hwShw, double hwHwt,
            double ngasChp,
            out double hwChp)
        {
            double temp = 0;
            if (string.Equals(tracking, "Thermal"))
                temp = Math.Min(CAP_CHP / EFF_CHP * HREC_CHP,
                    hWn + hwAbs - hwShw - hwHwt - hwEhp); //hwN - hwEhp + hwAbs - hwShw - hwHwt
            if (string.Equals(tracking, "Electrical"))
                temp = ngasChp * HREC_CHP;
            if (temp > hWn + hwAbs - hwShw - hwHwt - hwEhp) // CHP is forced to produce more energy than necessary
            {
                // Send Excess heat to Tank
                SHW_BAL[i] = SHW_BAL[i] + temp;
                if (i > 0)
                    eqTANK_CHG_n(TANK_CHG_n[i - 1], SHW_BAL[i], T_AMB_n[i], out TANK_CHG_n[i]);
                temp = 0; // force to zero becasue hwChp supplied to project is null; it only served to charge the tank
                //LogMessageToFile("The CHP plant was forced to produce more energy than needed.", i);

            }

            hwChp = temp;
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
            double temp = 0;
            if (string.Equals(tracking, "Thermal"))
                temp = hwChp / HREC_CHP;
            if (string.Equals(tracking, "Electrical"))
                temp = elecChp / EFF_CHP;
            ngasChp = temp;
        }

        /// <summary>
        ///     Equation 15/16 : The the electricity generated by the CHP plant
        /// </summary>
        /// <param name="tracking"></param>
        /// <param name="ngasChp"></param>
        /// <param name="elecN"></param>
        /// <param name="elecRen"></param>
        /// <param name="elecBat"></param>
        /// <param name="elecEch"></param>
        /// <param name="elecEhp"></param>
        /// <param name="elecChp"></param>
        private void eqELEC_CHP(string tracking, double ngasChp, double elecN, double elecRen, double elecBat,
            double elecEch, double elecEhp,
            out double elecChp)
        {
            double temp = 0;
            if (string.Equals(tracking, "Thermal"))
            {
                temp = ngasChp * EFF_CHP;
                if (temp > elecN + elecEhp + elecEch - elecRen - elecBat)
                {
                    // Send excess elec to Battery
                    ELEC_BAL[i] = ELEC_BAL[i] + temp;
                    if (i > 0)
                        eqBAT_CHG_n(BAT_CHG_n[i-1], ELEC_BAL[i], out BAT_CHG_n[i]);
                    temp = 0; // force to zero becasue elecChp supplied to project is null; it only served to charge the battery
                }
            }

            if (string.Equals(tracking, "Electrical"))
                temp = GetSmallestNonNegative(CAP_CHP,
                    elecN + elecEch + elecEhp - elecRen -
                    elecBat); // todo CHP should cover all electrical loads, if it can.
            elecChp = temp;
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
                return 0;
            if (a < 0 && b >= 0)
                return 0;
            return 0;
        }

        private static double GetHighestNonNegative(double a, double b)
        {
            if (a >= 0 && b >= 0)
                return Math.Max(a, b);
            if (a >= 0 && b < 0)
                return a;
            if (a < 0 && b >= 0)
                return b;
            return 0;
        }

        #endregion

        #region Results Array

        /// <summary>
        ///     eq11 The battery charge for each hour
        /// </summary>
        private readonly double[] BAT_CHG_n = new double[8760];

        /// <summary>
        ///     Chilled Water met by Electric Chillers
        /// </summary>
        private readonly double[] CHW_ECH = new double[8760];

        /// <summary>
        ///     Chilled Water met by Absorption Chiller
        /// </summary>
        private readonly double[] CHW_ABS = new double[8760];

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
        private readonly double[] ELEC_PROJ = new double[8760];

        /// <summary>
        ///     Hourly purchased natural gas
        /// </summary>
        private readonly double[] NGAS_PROJ = new double[8760];

        private void SetResultsArraystoZero()
        {
            Array.Clear(BAT_CHG_n, 0, BAT_CHG_n.Length);
            Array.Clear(ELEC_BAT, 0, ELEC_BAT.Length);
            Array.Clear(ELEC_CHP, 0, ELEC_CHP.Length);
            Array.Clear(ELEC_ECH, 0, ELEC_ECH.Length);
            Array.Clear(ELEC_EHP, 0, ELEC_EHP.Length);
            Array.Clear(ELEC_PV, 0, ELEC_PV.Length);
            Array.Clear(ELEC_REN, 0, ELEC_REN.Length);
            Array.Clear(ELEC_WND, 0, ELEC_WND.Length);
            Array.Clear(ELEC_BAL, 0, ELEC_BAL.Length);
            Array.Clear(HW_ABS, 0, HW_ABS.Length);
            Array.Clear(HW_EHP, 0, HW_EHP.Length);
            Array.Clear(HW_CHP, 0, HW_CHP.Length);
            Array.Clear(HW_HWT, 0, HW_HWT.Length);
            Array.Clear(HW_SHW, 0, HW_SHW.Length);
            Array.Clear(HW_NGB, 0, HW_NGB.Length);
            Array.Clear(NGAS_CHP, 0, NGAS_CHP.Length);
            Array.Clear(NGAS_NGB, 0, NGAS_NGB.Length);
            Array.Clear(TANK_CHG_n, 0, TANK_CHG_n.Length);
            Array.Clear(SHW_BAL, 0, SHW_BAL.Length);
            Array.Clear(ELEC_PROJ, 0, ELEC_PROJ.Length);
            Array.Clear(NGAS_PROJ, 0, NGAS_PROJ.Length);
        }

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