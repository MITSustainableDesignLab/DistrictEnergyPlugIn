using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using CsvHelper;
using DistrictEnergy.ViewModels;
using EnergyPlusWeather;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Umi.Core;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

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
            ResultsArray = new ResultsArray();
            AllDistrictDemand = new DistrictDemand();
            SimConstants = new SimConstants();
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

        private int numberTimesteps { get; set; }

        /// <summary>
        ///     This is the routine that goeas though all the eqautions one timestep at a time
        /// </summary>
        private void MainSimulation()
        {
            i = 0;
            StatusBar.ShowProgressMeter(0, numberTimesteps, "Solving Thermal Plant Components", true, true);
            for (; i < numberTimesteps; i++)
            {
                //if (i == 3878)
                //    Debugger.Break();
                eqHW_ABS(AllDistrictDemand.CHW_n[i], out ResultsArray.HW_ABS[i], out ResultsArray.CHW_ABS[i]); //OK
                eqELEC_ECH(AllDistrictDemand.CHW_n[i], ResultsArray.EhpEvap[i], out ResultsArray.ELEC_ECH[i], out ResultsArray.CHW_ECH[i], out ResultsArray.CHW_EHPevap[i]); //OK
                eqHW_SHW(AllDistrictDemand.RAD_n[i], AllDistrictDemand.HW_n[i], ResultsArray.HW_ABS[i],
                    out ResultsArray.HW_SHW[i],
                    out ResultsArray.SHW_BAL[i]); // OK todo Surplus energy from the chp should go in the battery
                eqELEC_PV(AllDistrictDemand.RAD_n[i], out ResultsArray.ELEC_PV[i]); // OK
                eqELEC_WND(AllDistrictDemand.WIND_n[i], out ResultsArray.ELEC_WND[i]); // OK
                if (i == 0) ResultsArray.TANK_CHG_n[i] = SimConstants.CapHwt * DistrictEnergy.Settings.TANK_START;
                if (i > 0)
                    eqTANK_CHG_n(ResultsArray.TANK_CHG_n[i - 1], ResultsArray.SHW_BAL[i], AllDistrictDemand.T_AMB_n[i],
                        out ResultsArray.TANK_CHG_n[i]); // OK
                eqHW_HWT(ResultsArray.TANK_CHG_n[i], AllDistrictDemand.HW_n[i], ResultsArray.HW_ABS[i],
                    ResultsArray.HW_SHW[i], out ResultsArray.HW_HWT[i]); // OK
                eqELEC_EHP(AllDistrictDemand.HW_n[i], ResultsArray.HW_ABS[i], ResultsArray.HW_SHW[i],
                    ResultsArray.HW_HWT[i], ResultsArray.HW_CHP[i], out ResultsArray.ELEC_EHP[i],
                    out ResultsArray.HW_EHP[i], out ResultsArray.EhpEvap[i]); // Will call HW_CHP even though its not calculated yet
                // Call ECH a second time to calculate new chiller load since heat pumps may have reduced the load
                eqELEC_ECH(AllDistrictDemand.CHW_n[i], ResultsArray.EhpEvap[i], out ResultsArray.ELEC_ECH[i], out ResultsArray.CHW_ECH[i], out ResultsArray.CHW_EHPevap[i]); //OK

                eqELEC_REN(ResultsArray.ELEC_PV[i], ResultsArray.ELEC_WND[i], AllDistrictDemand.ELEC_n[i],
                    ResultsArray.ELEC_ECH[i], ResultsArray.ELEC_EHP[i], out ResultsArray.ELEC_REN[i],
                    out ResultsArray.ELEC_BAL[i]); // OK
                if (i == 0) ResultsArray.BAT_CHG_n[0] = SimConstants.CapBat * DistrictEnergy.Settings.BAT_START;
                if (i > 0)
                    eqBAT_CHG_n(ResultsArray.BAT_CHG_n[i - 1], ResultsArray.ELEC_BAL[i],
                        out ResultsArray.BAT_CHG_n[i]); // OK
                eqELEC_BAT(ResultsArray.BAT_CHG_n[i], AllDistrictDemand.ELEC_n[i], ResultsArray.ELEC_ECH[i],
                    ResultsArray.ELEC_EHP[i], out ResultsArray.ELEC_BAT[i], ResultsArray.ELEC_REN[i],
                    ResultsArray.ELEC_CHP[i]); // OK

                if (string.Equals(DistrictEnergy.Settings.TMOD_CHP, "Thermal"))
                {
                    // ignore NgasChp
                    eqHW_CHP(DistrictEnergy.Settings.TMOD_CHP, AllDistrictDemand.HW_n[i], ResultsArray.HW_EHP[i], ResultsArray.HW_ABS[i],
                        ResultsArray.HW_SHW[i], ResultsArray.HW_HWT[i], ResultsArray.NGAS_CHP[i],
                        out ResultsArray.HW_CHP[i]);
                    // ignore Elec_Chp
                    eqNGAS_CHP(DistrictEnergy.Settings.TMOD_CHP, ResultsArray.HW_CHP[i], ResultsArray.ELEC_CHP[i],
                        out ResultsArray.NGAS_CHP[i]);
                    // ignore ElecN, ElecRen, ElecBat
                    eqELEC_CHP(DistrictEnergy.Settings.TMOD_CHP, ResultsArray.NGAS_CHP[i], AllDistrictDemand.ELEC_n[i],
                        ResultsArray.ELEC_REN[i], ResultsArray.ELEC_BAT[i], ResultsArray.ELEC_ECH[i],
                        ResultsArray.ELEC_EHP[i],
                        out ResultsArray.ELEC_CHP[i]);
                }

                if (string.Equals(DistrictEnergy.Settings.TMOD_CHP, "Electrical"))
                {
                    // ignore NgasChp
                    eqELEC_CHP(DistrictEnergy.Settings.TMOD_CHP, ResultsArray.NGAS_CHP[i], AllDistrictDemand.ELEC_n[i],
                        ResultsArray.ELEC_REN[i], ResultsArray.ELEC_BAT[i], ResultsArray.ELEC_ECH[i],
                        ResultsArray.ELEC_EHP[i],
                        out ResultsArray.ELEC_CHP[i]);
                    // ignore HwChp
                    eqNGAS_CHP(DistrictEnergy.Settings.TMOD_CHP, ResultsArray.HW_CHP[i], ResultsArray.ELEC_CHP[i],
                        out ResultsArray.NGAS_CHP[i]);
                    // ignore HwN, HwEhp, HwAbs, HwShw, HwHwt
                    eqHW_CHP(DistrictEnergy.Settings.TMOD_CHP, AllDistrictDemand.HW_n[i], ResultsArray.HW_EHP[i], ResultsArray.HW_ABS[i],
                        ResultsArray.HW_SHW[i], ResultsArray.HW_HWT[i], ResultsArray.NGAS_CHP[i],
                        out ResultsArray.HW_CHP[i]);
                }

                eqNGAS_NGB(AllDistrictDemand.HW_n[i], ResultsArray.HW_EHP[i], ResultsArray.HW_ABS[i],
                    ResultsArray.HW_SHW[i], ResultsArray.HW_HWT[i], ResultsArray.HW_CHP[i],
                    out ResultsArray.NGAS_NGB[i],
                    out ResultsArray.HW_NGB[i]);
                eqELEC_proj(AllDistrictDemand.ELEC_n[i], ResultsArray.ELEC_REN[i], ResultsArray.ELEC_BAT[i],
                    ResultsArray.ELEC_CHP[i], ResultsArray.ELEC_EHP[i], ResultsArray.ELEC_ECH[i],
                    out ResultsArray.ELEC_PROJ[i]);
                eqNGAS_proj(ResultsArray.NGAS_NGB[i], ResultsArray.NGAS_CHP[i], out ResultsArray.NGAS_PROJ[i]);
                StatusBar.UpdateProgressMeter(i, true);
            }

            ResultsArray.OnResultsChanged(EventArgs.Empty);
            RhinoApp.WriteLine("Distric Energy Simulation complete");
            StatusBar.HideProgressMeter();
        }

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
                umiContext.GetObjects().Where(o => o.Data.Any(x=>x.Value.Data.Count == 8760)).ToList();
            if (contextBuildings.Count == 0)
            {
                MessageBox.Show(
                    "There are no buildings with hourly results. Please Rerun the Energy Module after turning on the Hourly Results in Advanced Options",
                    "Cannot continue with District simulation");
                return Result.Failure;
            }

            AllDistrictDemand.CHW_n = GetHourlyChilledWaterProfile(contextBuildings);
            AllDistrictDemand.HW_n = GetHourlyHotWaterLoadProfile(contextBuildings);
            AllDistrictDemand.ELEC_n = GetHourlyElectricalLoadProfile(contextBuildings);
            StatusBar.HideProgressMeter();
            AllDistrictDemand.RAD_n = GetHourlyLocationSolarRadiation(umiContext).ToArray();
            AllDistrictDemand.WIND_n = GetHourlyLocationWind(umiContext).ToArray();
            AllDistrictDemand.T_AMB_n = GetHourlyLocationAmbiantTemp(umiContext).ToArray();


            numberTimesteps = AllDistrictDemand.HW_n.Length;

            RhinoApp.WriteLine(
                $"Calculated...\n{AllDistrictDemand.CHW_n.Length} datapoints for ColdWater profile\n{AllDistrictDemand.HW_n.Count()} datapoints for HotWater\n{AllDistrictDemand.ELEC_n.Count()} datapoints for Electricity\n{AllDistrictDemand.RAD_n.Count()} datapoints for Solar Frad\n{AllDistrictDemand.WIND_n.Count()} datapoints for WindSpeed");

            // Go Hour by hour and parse through the simulation routine
            SetResultsArraystoZero();
            DeleteLogFile();
            SimConstants.CalculateConstants();
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
            return a.HourlyWeatherDataRawList.Select(b => (double) b.DB); // C
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
                Headers.Add("DateTime"); // 1
                Headers.Add("Hour"); // 2
                Headers.Add("BAT_CHG_n"); // 3
                Headers.Add("ELEC_BAT"); // 4
                Headers.Add("ELEC_CHP"); // 5
                Headers.Add("ELEC_ECH"); // 6
                Headers.Add("ELEC_EHP"); // 7
                Headers.Add("ELEC_PV"); // 8
                Headers.Add("ELEC_REN"); // 9
                Headers.Add("ELEC_WND"); // 10
                Headers.Add("ELEC_BAL"); // 11
                Headers.Add("HW_ABS"); // 12
                Headers.Add("HW_EHP"); // 13
                Headers.Add("HW_CHP"); // 14
                Headers.Add("HW_HWT"); // 15
                Headers.Add("HW_SHW"); // 16
                Headers.Add("HW_NGB"); // 17
                Headers.Add("NGAS_CHP"); // 18
                Headers.Add("NGAS_NGB"); // 19
                Headers.Add("TANK_CHG_n"); // 20
                Headers.Add("SHW_BAL"); // 21
                Headers.Add("ELEC_PROJ"); // 22
                Headers.Add("NGAS_PROJ"); // 23
                Headers.Add("CHW_n"); // 24
                Headers.Add("HW_n"); // 25
                Headers.Add("ELEC_n"); // 26
                Headers.Add("CHW_ABS"); // 27
                Headers.Add("CHW_ECH"); // 28
                Headers.Add("CHW_EHPEvap"); // 29

                foreach (var header in Headers) csvWriter.WriteField(header);

                csvWriter.NextRecord();

                StatusBar.HideProgressMeter();
                StatusBar.ShowProgressMeter(0, 8760, "Saving Results to CSV", true, true);
                var dateTime = new DateTime(2017, 1, 1, 0, 0, 0);

                for (var i = 0; i < 8760; i++)
                {
                    csvWriter.WriteField(dateTime); // 1
                    csvWriter.WriteField(i); // 2
                    csvWriter.WriteField(ResultsArray.BAT_CHG_n[i]); // 3
                    csvWriter.WriteField(ResultsArray.ELEC_BAT[i]); // 4
                    csvWriter.WriteField(ResultsArray.ELEC_CHP[i]); // 5
                    csvWriter.WriteField(ResultsArray.ELEC_ECH[i]); // 6
                    csvWriter.WriteField(ResultsArray.ELEC_EHP[i]); // 7
                    csvWriter.WriteField(ResultsArray.ELEC_PV[i]); // 8
                    csvWriter.WriteField(ResultsArray.ELEC_REN[i]); // 9
                    csvWriter.WriteField(ResultsArray.ELEC_WND[i]); // 10
                    csvWriter.WriteField(ResultsArray.ELEC_BAL[i]); // 11
                    csvWriter.WriteField(ResultsArray.HW_ABS[i]); // 12
                    csvWriter.WriteField(ResultsArray.HW_EHP[i]); // 13
                    csvWriter.WriteField(ResultsArray.HW_CHP[i]); // 14
                    csvWriter.WriteField(ResultsArray.HW_HWT[i]); // 15
                    csvWriter.WriteField(ResultsArray.HW_SHW[i]); // 16
                    csvWriter.WriteField(ResultsArray.HW_NGB[i]); // 17
                    csvWriter.WriteField(ResultsArray.NGAS_CHP[i]); // 18
                    csvWriter.WriteField(ResultsArray.NGAS_NGB[i]); // 19
                    csvWriter.WriteField(ResultsArray.TANK_CHG_n[i]); // 20
                    csvWriter.WriteField(ResultsArray.SHW_BAL[i]); // 21
                    csvWriter.WriteField(ResultsArray.ELEC_PROJ[i]); // 22
                    csvWriter.WriteField(ResultsArray.NGAS_PROJ[i]); // 23
                    csvWriter.WriteField(AllDistrictDemand.CHW_n[i]); // 24
                    csvWriter.WriteField(AllDistrictDemand.HW_n[i]); // 25
                    csvWriter.WriteField(AllDistrictDemand.ELEC_n[i]); // 26
                    csvWriter.WriteField(ResultsArray.CHW_ABS[i]); // 27
                    csvWriter.WriteField(ResultsArray.CHW_ECH[i]); // 28
                    csvWriter.WriteField(ResultsArray.CHW_EHPevap[i]); // 29

                    csvWriter.NextRecord();
                    dateTime = dateTime.AddHours(1);
                    StatusBar.UpdateProgressMeter(i, true);
                }

                StatusBar.HideProgressMeter();
                writer.Close();
            }

            RhinoApp.WriteLine(string.Format(@"CSV file successfully written to {0}", file_name));
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

        #region Equation 1 to 18

        /// <summary>
        ///     Equation 1 : The hot water required to generate project chilled water
        /// </summary>
        /// <param name="chwN">Hourly chilled water load profile (kWh)</param>
        /// <param name="hwAbs">Hot water required for Absorption Chiller</param>
        /// <param name="chwAbs"></param>
        private void eqHW_ABS(double chwN, out double hwAbs, out double chwAbs)
        {
            hwAbs = Math.Min(chwN, SimConstants.CapAbs) / DistrictEnergy.Settings.CCOP_ABS;
            chwAbs = Math.Min(chwN, SimConstants.CapAbs);
        }

        /// <summary>
        ///     Any loads in surplus of the absorption chiller capacity are met by electric chillers for each hour. The results
        ///     from this component are hourly profiles for electricity consumption, (ELEC_ECH), to generate project chilled water,
        ///     and the annual total can be expressed as:
        /// </summary>
        /// <param name="chwN">Hourly chilled water load profile (kWh)</param>
        /// <param name="ehpEvap">All evaporator-side energy</param>
        /// <param name="elecEch">Electricity Consumption to generate chilled water from chillers</param>
        /// <param name="chwEch"></param>
        /// <param name="chwEhpEvap"></param>
        private void eqELEC_ECH(double chwN, double ehpEvap, out double elecEch, out double chwEch,
            out double chwEhpEvap)
        {
            if (chwN > SimConstants.CapAbs)
            {
                elecEch = Math.Max(chwN - SimConstants.CapAbs - ehpEvap, 0) / DistrictEnergy.Settings.CCOP_ECH;
                chwEch = Math.Max(chwN - SimConstants.CapAbs - ehpEvap, 0);

                if (ehpEvap > chwN - SimConstants.CapAbs)
                    chwEhpEvap = chwN - SimConstants.CapAbs;
                else
                {
                    chwEhpEvap = ehpEvap;
                }
            }
            else
            {
                elecEch = 0;
                chwEch = 0;
                chwEhpEvap = 0;
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
        /// <param name="ehpEvap"></param>
        private void eqELEC_EHP(double hwN, double hwAbs, double hwShw,
            double hwHwt, double hwChp, out double elecEhp, out double hwEhp, out double ehpEvap)
        {
            elecEhp = GetSmallestNonNegative(hwN + hwAbs - hwShw - hwHwt - hwChp, SimConstants.CapEhp) / DistrictEnergy.Settings.HCOP_EHP;
            hwEhp = GetSmallestNonNegative(hwN + hwAbs - hwShw - hwHwt - hwChp, SimConstants.CapEhp);
            if (DistrictEnergy.Settings.UseEhpEvap)
                ehpEvap = hwEhp * (1 - 1 / DistrictEnergy.Settings.HCOP_EHP);
            else
                ehpEvap = 0;
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
            ngasNgb = Math.Max(hwN - hwEhp + hwAbs - hwShw - hwHwt - hwChp, 0) / DistrictEnergy.Settings.EFF_NGB; // If chp produces more energy than needed, ngas will be 0.
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
            hwShw = Math.Min(radN * SimConstants.AreaShw * DistrictEnergy.Settings.EFF_SHW * DistrictEnergy.Settings.UTIL_SHW * (1 - DistrictEnergy.Settings.LOSS_SHW), hwN + hwAbs);
            solarBalance = radN * SimConstants.AreaShw * DistrictEnergy.Settings.EFF_SHW * DistrictEnergy.Settings.UTIL_SHW * (1 - DistrictEnergy.Settings.LOSS_SHW) - hwN - hwAbs;
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
                tankChgN = Math.Max(previousTankChgN + shwBal, GetHighestNonNegative(previousTankChgN - SimConstants.DchgrHwt, 0));
                SimConstants.LossHwt = (-4E-5 * tAmb + 0.0024) * Math.Pow(tankChgN, -1 / 3); // todo tanklosses-to-tAmb relation
                tankChgN = tankChgN * (1 - SimConstants.LossHwt);
            }
            else if (shwBal > 0) // We are charging the tank
            {
                tankChgN = GetSmallestNonNegative(previousTankChgN + shwBal,
                    GetSmallestNonNegative(previousTankChgN + SimConstants.ChgrHwt, SimConstants.CapHwt));
                SimConstants.LossHwt = (-4E-5 * tAmb + 0.0024) * Math.Pow(tankChgN, -1 / 3);
                tankChgN = tankChgN * (1 - SimConstants.LossHwt);
            }
            else // We are not doing anything, but losses still occur
            {
                tankChgN = previousTankChgN *
                           (1 - SimConstants.LossHwt); // Contrary to the Grasshopper code, the tank loses energy to the environnement even when not used.
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
            hwHwt = GetSmallestNonNegative(hwN + hwAbs - hwShw, GetSmallestNonNegative(tankChgN, SimConstants.DchgrHwt));
        }

        /// <summary>
        ///     Equation 8 : The PV total electricity generation
        /// </summary>
        /// <param name="radN"></param>
        /// <param name="elecPv"></param>
        private void eqELEC_PV(double radN, out double elecPv)
        {
            elecPv = radN * SimConstants.AreaPv * DistrictEnergy.Settings.EFF_PV * DistrictEnergy.Settings.UTIL_PV * (1 - DistrictEnergy.Settings.LOSS_PV);
        }

        /// <summary>
        ///     Equation 9 : The annual electricity generation for Wind Turbines
        /// </summary>
        /// <param name="windN"></param>
        /// <param name="elecWnd"></param>
        private void eqELEC_WND(double windN, out double elecWnd)
        {
            elecWnd = 0.6375 * Math.Pow(windN, 3) * DistrictEnergy.Settings.ROT_WND * SimConstants.NumWnd * DistrictEnergy.Settings.COP_WND * (1 - DistrictEnergy.Settings.LOSS_WND) /
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
            if (elecBalance < 0
            ) // We are discharging the battery; Not all the elecBalance can be fed to the batt because of the LOSS_BAT parameter.
                batChgN = Math.Max(previousBatChgN + elecBalance * (1 - DistrictEnergy.Settings.LOSS_BAT),
                    GetHighestNonNegative(previousBatChgN - SimConstants.DchgBat, 0));
            else if (elecBalance > 0) // We are charging the battery
                batChgN = GetSmallestNonNegative(previousBatChgN + elecBalance * (1 - DistrictEnergy.Settings.LOSS_BAT),
                    GetSmallestNonNegative(previousBatChgN + SimConstants.ChgrBat, SimConstants.CapBat));
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
            elecBat = GetSmallestNonNegative(elecN + elecEch + elecEhp - elecRen - elecChp,
                GetSmallestNonNegative(batChgN, SimConstants.DchgBat));
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
                temp = Math.Min(SimConstants.CapChpElec / DistrictEnergy.Settings.EFF_CHP * DistrictEnergy.Settings.HREC_CHP,
                    hWn + hwAbs - hwShw - hwHwt - hwEhp); //hwN - hwEhp + hwAbs - hwShw - hwHwt
            if (string.Equals(tracking, "Electrical"))
                temp = ngasChp * DistrictEnergy.Settings.HREC_CHP;
            if (temp > hWn + hwAbs - hwShw - hwHwt - hwEhp) // CHP is forced to produce more energy than necessary
            {
                // Send Excess heat to Tank
                ResultsArray.SHW_BAL[i] = ResultsArray.SHW_BAL[i] + temp;
                if (i > 0)
                    eqTANK_CHG_n(ResultsArray.TANK_CHG_n[i - 1], ResultsArray.SHW_BAL[i], AllDistrictDemand.T_AMB_n[i],
                        out ResultsArray.TANK_CHG_n[i]);
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
                temp = hwChp / DistrictEnergy.Settings.HREC_CHP;
            if (string.Equals(tracking, "Electrical"))
                temp = elecChp / DistrictEnergy.Settings.EFF_CHP;
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
                temp = ngasChp * DistrictEnergy.Settings.EFF_CHP;
                if (temp > elecN + elecEhp + elecEch - elecRen - elecBat)
                {
                    // Send excess elec to Battery
                    ResultsArray.ELEC_BAL[i] = ResultsArray.ELEC_BAL[i] + temp;
                    if (i > 0)
                        eqBAT_CHG_n(ResultsArray.BAT_CHG_n[i - 1], ResultsArray.ELEC_BAL[i],
                            out ResultsArray.BAT_CHG_n[i]);
                    temp = 0; // force to zero becasue elecChp supplied to project is null; it only served to charge the battery
                }
            }

            if (string.Equals(tracking, "Electrical"))
                temp = GetSmallestNonNegative(SimConstants.CapChpElec,
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

        private void SetResultsArraystoZero()
        {
            Array.Clear(ResultsArray.BAT_CHG_n, 0, ResultsArray.BAT_CHG_n.Length);
            Array.Clear(ResultsArray.CHW_ABS, 0, ResultsArray.CHW_ABS.Length);
            Array.Clear(ResultsArray.CHW_ECH, 0, ResultsArray.CHW_ECH.Length);
            Array.Clear(ResultsArray.ELEC_BAT, 0, ResultsArray.ELEC_BAT.Length);
            Array.Clear(ResultsArray.ELEC_CHP, 0, ResultsArray.ELEC_CHP.Length);
            Array.Clear(ResultsArray.ELEC_ECH, 0, ResultsArray.ELEC_ECH.Length);
            Array.Clear(ResultsArray.ELEC_EHP, 0, ResultsArray.ELEC_EHP.Length);
            Array.Clear(ResultsArray.ELEC_PV, 0, ResultsArray.ELEC_PV.Length);
            Array.Clear(ResultsArray.ELEC_REN, 0, ResultsArray.ELEC_REN.Length);
            Array.Clear(ResultsArray.ELEC_WND, 0, ResultsArray.ELEC_WND.Length);
            Array.Clear(ResultsArray.ELEC_BAL, 0, ResultsArray.ELEC_BAL.Length);
            Array.Clear(ResultsArray.HW_ABS, 0, ResultsArray.HW_ABS.Length);
            Array.Clear(ResultsArray.HW_EHP, 0, ResultsArray.HW_EHP.Length);
            Array.Clear(ResultsArray.HW_CHP, 0, ResultsArray.HW_CHP.Length);
            Array.Clear(ResultsArray.HW_HWT, 0, ResultsArray.HW_HWT.Length);
            Array.Clear(ResultsArray.HW_SHW, 0, ResultsArray.HW_SHW.Length);
            Array.Clear(ResultsArray.HW_NGB, 0, ResultsArray.HW_NGB.Length);
            Array.Clear(ResultsArray.NGAS_CHP, 0, ResultsArray.NGAS_CHP.Length);
            Array.Clear(ResultsArray.NGAS_NGB, 0, ResultsArray.NGAS_NGB.Length);
            Array.Clear(ResultsArray.TANK_CHG_n, 0, ResultsArray.TANK_CHG_n.Length);
            Array.Clear(ResultsArray.SHW_BAL, 0, ResultsArray.SHW_BAL.Length);
            Array.Clear(ResultsArray.ELEC_PROJ, 0, ResultsArray.ELEC_PROJ.Length);
            Array.Clear(ResultsArray.NGAS_PROJ, 0, ResultsArray.NGAS_PROJ.Length);
            Array.Clear(ResultsArray.EhpEvap, 0, ResultsArray.EhpEvap.Length);
            Array.Clear(ResultsArray.CHW_EHPevap, 0, ResultsArray.CHW_EHPevap.Length);
        }

        public ResultsArray ResultsArray { get; }

        public DistrictDemand AllDistrictDemand { get; }
        public SimConstants SimConstants { get; }

        #endregion

        #region AvailableSettings

        #endregion
    }

    public class DistrictDemand
    {
        /// <summary>
        ///     Hourly chilled water load profile (kWh)
        /// </summary>
        public double[] CHW_n = new double[8760];

        /// <summary>
        ///     Hourly electricity load profile (kWh)
        /// </summary>
        public double[] ELEC_n = new double[8760];

        /// <summary>
        ///     Hourly hot water load profile (kWh)
        /// </summary>
        public double[] HW_n = new double[8760];

        /// <summary>
        ///     Hourly Global Solar Radiation from EPW file (kWh/m2)
        /// </summary>
        public double[] RAD_n = new double[8760];

        /// <summary>
        ///     Hourly Ambiant temperature (C)
        /// </summary>
        public double[] T_AMB_n = new double[8760];

        /// <summary>
        ///     Hourly location wind speed data (m/s)
        /// </summary>
        public double[] WIND_n = new double[8760];
    }

    public class ResultsArray
    {
        /// <summary>
        ///     eq11 The battery charge for each hour
        /// </summary>
        public readonly double[] BAT_CHG_n = new double[8760];

        /// <summary>
        ///     Chilled Water met by Absorption Chiller
        /// </summary>
        public readonly double[] CHW_ABS = new double[8760];

        /// <summary>
        ///     Chilled Water met by Electric Chillers
        /// </summary>
        public readonly double[] CHW_ECH = new double[8760];

        /// <summary>
        ///     Electricity generation balance from renewables only
        /// </summary>
        public readonly double[] ELEC_BAL = new double[8760];

        /// <summary>
        ///     eq12 Demand met by the battery bank
        /// </summary>
        public readonly double[] ELEC_BAT = new double[8760];

        /// <summary>
        ///     eq15/16 Electricity generated by CHP plant
        /// </summary>
        public readonly double[] ELEC_CHP = new double[8760];

        /// <summary>
        ///     eq2 Electricity Consumption to generate chilled water from chillers
        /// </summary>
        public readonly double[] ELEC_ECH = new double[8760];

        /// <summary>
        ///     eq3 Electricity consumption required to generate hot water from HPs
        /// </summary>
        public readonly double[] ELEC_EHP = new double[8760];

        /// <summary>
        ///     Hourly purchased grid electricity
        /// </summary>
        public readonly double[] ELEC_PROJ = new double[8760];

        /// <summary>
        ///     eq8 Total PV electricity generation
        /// </summary>
        public readonly double[] ELEC_PV = new double[8760];

        /// <summary>
        ///     eq10 Total nenewable electricity generation
        /// </summary>
        public readonly double[] ELEC_REN = new double[8760];

        /// <summary>
        ///     eq9 Total Wind electricity generation
        /// </summary>
        public readonly double[] ELEC_WND = new double[8760];

        /// <summary>
        ///     eq1 Hot water required for Absorption Chiller
        /// </summary>
        public readonly double[] HW_ABS = new double[8760];

        /// <summary>
        ///     eq13 Heating energy recovered from the combined heat and power plant and supplied to the project
        /// </summary>
        public readonly double[] HW_CHP = new double[8760];

        /// <summary>
        ///     Hot water met by electric heat pumps
        /// </summary>
        public readonly double[] HW_EHP = new double[8760];

        /// <summary>
        ///     eq7 Demand met by hot water tanks
        /// </summary>
        public readonly double[] HW_HWT = new double[8760];

        /// <summary>
        ///     Hot Water produced by Natural Gas Boilers
        /// </summary>
        public readonly double[] HW_NGB = new double[8760];

        /// <summary>
        ///     eq5 Total Solar Hot Water generation to meet building loads
        /// </summary>
        public readonly double[] HW_SHW = new double[8760];

        /// <summary>
        ///     eq14/17 Natural gas consumed by CHP plant
        /// </summary>
        public readonly double[] NGAS_CHP = new double[8760];

        /// <summary>
        ///     eq4 Boiler natural gas consumption to generate project hot water
        /// </summary>
        public readonly double[] NGAS_NGB = new double[8760];

        /// <summary>
        ///     Hourly purchased natural gas
        /// </summary>
        public readonly double[] NGAS_PROJ = new double[8760];

        /// <summary>
        ///     Hour Surplus
        /// </summary>
        public readonly double[] SHW_BAL = new double[8760];

        /// <summary>
        ///     eq6 The tank charge for each hour [kWh]
        /// </summary>
        public readonly double[] TANK_CHG_n = new double[8760];

        public event EventHandler ResultsChanged;
        public readonly double[] EhpEvap = new double[8760];
        public readonly double[] CHW_EHPevap = new double[8760];

        protected internal virtual void OnResultsChanged(EventArgs e)
        {
            var handler = ResultsChanged;
            handler?.Invoke(this, e);
        }
    }

    public class Settings
    {
        public static double CCOP_ECH
        {
            get { return PlantSettingsViewModel.Instance.CCOP_ECH; }
        }

        public static double EFF_NGB
        {
            get { return PlantSettingsViewModel.Instance.EFF_NGB / 100; }
        }

        internal static double OFF_ABS
        {
            get { return PlantSettingsViewModel.Instance.OFF_ABS / 100; }
        }

        public static double CCOP_ABS
        {
            get { return PlantSettingsViewModel.Instance.CCOP_ABS; }
        }

        internal static double AUT_BAT
        {
            get { return PlantSettingsViewModel.Instance.AUT_BAT; }
        }

        public static double LOSS_BAT
        {
            get { return PlantSettingsViewModel.Instance.LOSS_BAT / 100; }
        }

        public static string TMOD_CHP
        {
            get { return PlantSettingsViewModel.Instance.TMOD_CHP.ToString(); }
        }

        internal static double OFF_CHP
        {
            get { return PlantSettingsViewModel.Instance.OFF_CHP / 100; }
        }

        public static double EFF_CHP
        {
            get { return PlantSettingsViewModel.Instance.EFF_CHP / 100; }
        }

        public static double HREC_CHP
        {
            get { return PlantSettingsViewModel.Instance.HREC_CHP / 100; }
        }

        internal static double OFF_EHP
        {
            get { return PlantSettingsViewModel.Instance.OFF_EHP / 100; }
        }

        public static double HCOP_EHP
        {
            get { return PlantSettingsViewModel.Instance.HCOP_EHP; }
        }

        internal static double AUT_HWT
        {
            get { return PlantSettingsViewModel.Instance.AUT_HWT; }
        }

        internal static double OFF_PV
        {
            get { return PlantSettingsViewModel.Instance.OFF_PV / 100; }
        }

        public static double UTIL_PV
        {
            get { return PlantSettingsViewModel.Instance.UTIL_PV / 100; }
        }

        public static double LOSS_PV
        {
            get { return PlantSettingsViewModel.Instance.LOSS_PV / 100; }
        }

        public static double EFF_PV
        {
            get { return PlantSettingsViewModel.Instance.EFF_PV / 100; }
        }

        /// <summary>
        ///     Collector efficiency (%)
        /// </summary>
        public static double EFF_SHW
        {
            get { return PlantSettingsViewModel.Instance.EFF_SHW / 100; }
        }

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        public static double LOSS_SHW
        {
            get { return PlantSettingsViewModel.Instance.LOSS_SHW / 100; }
        }

        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        internal static double OFF_SHW
        {
            get { return PlantSettingsViewModel.Instance.OFF_SHW / 100; }
        }

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        public static double UTIL_SHW
        {
            get { return PlantSettingsViewModel.Instance.UTIL_SHW / 100; }
        }

        internal static double CIN_WND
        {
            get { return PlantSettingsViewModel.Instance.CIN_WND; }
        }

        public static double COP_WND
        {
            get { return PlantSettingsViewModel.Instance.EFF_WND / 100; }
        }

        internal static double COUT_WND
        {
            get { return PlantSettingsViewModel.Instance.COUT_WND; }
        }

        internal static double OFF_WND
        {
            get { return PlantSettingsViewModel.Instance.OFF_WND / 100; }
        }

        public static double ROT_WND
        {
            get { return PlantSettingsViewModel.Instance.ROT_WND; }
        }

        public static double LOSS_WND
        {
            get { return PlantSettingsViewModel.Instance.LOSS_WND / 100; }
        }

        /// <summary>
        ///     The Tank charged state at the begining of the simulation
        /// </summary>
        public static double TANK_START
        {
            get { return PlantSettingsViewModel.Instance.TANK_START / 100; }
        }

        /// <summary>
        ///     The Battery charged state at the begining of the simulation
        /// </summary>
        public static double BAT_START
        {
            get { return PlantSettingsViewModel.Instance.BAT_START / 100; }
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

        public static bool UseEhpEvap {
            get { return PlantSettingsViewModel.Instance.UseEhpEvap; }
        }
    }

    public class SimConstants : INotifyPropertyChanged
    {
        private double _dchgrHwt;
        private double _dchgBat;
        private double _capAbs;
        private double _capEhp;
        private double _capBat;
        private double _capHwt;
        private double _capChpElec;
        private double _areaShw;
        private double _areaPv;
        private double _numWnd;
        private double _chgrHwt;
        private double _chgrBat;
        private double _lossHwt;
        private double _capNgb;
        private double _capChpHeat;
        private double _capShw;
        private double _capEch;
        private double _capElecProj;
        private double _capPv;
        private double _capWnd;

        public SimConstants()
        {
            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
        }

        private void CalculateConstants(object sender, EventArgs e)
        {
            CalculateConstants();
        }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += CalculateConstants;
        }

        /// <summary>
        ///     Cooling capacity of absorption chillers (kW)
        /// </summary>
        public double CapAbs
        {
            get => _capAbs;
            set
            {
                _capAbs = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapAbs)));
            }
        }

        /// <summary>
        ///     Capacity of Electrical Heat Pumps
        /// </summary>
        public double CapEhp
        {
            get => _capEhp;
            set
            {
                _capEhp = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapEhp)));
            }
        }

        /// <summary>
        ///     Capacity of Battery, defined as the everage demand times the desired autonomy
        /// </summary>
        public double CapBat
        {
            get => _capBat;
            set
            {
                _capBat = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapBat)));
            }
        }

        /// <summary>
        ///     Capacity of Hot Water Tank, defined as the everage demand times the desired autonomy
        /// </summary>
        public double CapHwt
        {
            get => _capHwt;
            set
            {
                _capHwt = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapHwt)));
            }
        }

        /// <summary>
        ///     Capacity of CHP plant
        /// </summary>
        public double CapChpElec
        {
            get => _capChpElec;
            set
            {
                _capChpElec = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapChpElec)));
            }
        }

        /// <summary>
        ///     Calculated required area of solar thermal collector (m^2)
        /// </summary>
        public double AreaShw
        {
            get => _areaShw;
            set
            {
                _areaShw = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AreaShw)));
            }
        }

        /// <summary>
        ///     Calculated required area of PV collectors
        /// </summary>
        public double AreaPv
        {
            get => _areaPv;
            set
            {
                _areaPv = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AreaPv)));
            }
        }

        /// <summary>
        ///     Number of turbines needed: Annual electricity needed divided by how much one turbine generates.
        ///     [Annual Energy that needs to be generated/(0.635 x Rotor Area X sum of cubes of all wind speeds within cut-in and
        ///     cut-out speeds x COP)]
        /// </summary>
        public double NumWnd
        {
            get => _numWnd;
            set
            {
                _numWnd = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumWnd)));
            }
        }

        /// <summary>
        ///     The Hot Water Tank Charge Rate (kWh / h)
        /// </summary>
        public double ChgrHwt
        {
            get => _chgrHwt;
            set
            {
                _chgrHwt = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChgrHwt)));
            }
        }

        /// <summary>
        ///     The Battery Charge Rate (kWh / h)
        /// </summary>
        public double ChgrBat
        {
            get => _chgrBat;
            set
            {
                _chgrBat = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChgrBat)));
            }
        }

        /// <summary>
        ///     Discharge rate of Hot Water Tank
        /// </summary>
        public double DchgrHwt
        {
            get => _dchgrHwt;
            set
            {
                _dchgrHwt = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DchgrHwt)));
            }
        }

        /// <summary>
        ///     Discharge rate of battery
        /// </summary>
        public double DchgBat
        {
            get => _dchgBat;
            set
            {
                _dchgBat = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DchgBat)));
            }
        }

        /// <summary>
        ///     Hot Water Tank Losses (dependant of the outdoor temperature)
        /// </summary>
        public double LossHwt
        {
            get => _lossHwt;
            set
            {
                _lossHwt = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossHwt)));
            }
        }
        /// <summary>
        ///     Capacity in hot water production (max over the year)
        /// </summary>
        public double CapNgb
        {
            get => _capNgb;
            set
            {
                _capNgb = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapNgb)));
            }
        }
        /// <summary>
        ///     Capacity of CHP plant in hotwater production (max over the year)
        /// </summary>
        public double CapChpHeat
        {
            get => _capChpHeat;
            set
            {
                _capChpHeat = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapChpHeat)));
            }
        }

        /// <summary>
        ///     Capacity of the solar hot water array
        /// </summary>
        public double CapShw
        {
            get => _capShw;
            set
            {
                _capShw = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapShw)));
            }
        }

        /// <summary>
        ///     Capacity of the Electric Chiller
        /// </summary>
        public double CapEch
        {
            get => _capEch;
            set
            {
                _capEch = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapEch)));
            }
        }

        /// <summary>
        ///     Capacity of the grid to supply the project
        /// </summary>
        public double CapElecProj
        {
            get => _capElecProj;
            set
            {
                _capElecProj = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapElecProj)));
            }
        }

        /// <summary>
        ///     Capacity of the PV array
        /// </summary>
        public double CapPv
        {
            get { return _capPv; }
            set
            {
                _capPv = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapPv)));
            }
        }

        /// <summary>
        ///     Capacity of the wind turbine field
        /// </summary>
        public double CapWnd
        {
            get { return _capWnd; }
            set
            {
                _capWnd = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapWnd)));
            }
        }

        /// <summary>
        ///     Calculates the necessary constants used in different equations
        /// </summary>
        public void CalculateConstants()
        {
            CapAbs = DHSimulateDistrictEnergy.Instance.AllDistrictDemand.CHW_n.Max() * Settings.OFF_ABS;
            CapEhp = DHSimulateDistrictEnergy.Instance.AllDistrictDemand.HW_n.Max() * Settings.OFF_EHP;
            CapBat = DHSimulateDistrictEnergy.Instance.AllDistrictDemand.ELEC_n.Average() * Settings.AUT_BAT * 24;
            CapHwt = DHSimulateDistrictEnergy.Instance.AllDistrictDemand.HW_n.Average() * Settings.AUT_HWT * 24; // todo Prendre jour moyen du mois max.
            CapChpElec = DHSimulateDistrictEnergy.Instance.AllDistrictDemand.ELEC_n.Max() * Settings.OFF_CHP;
            CapChpHeat = DHSimulateDistrictEnergy.Instance.ResultsArray.HW_CHP.Max();
            CapNgb = DHSimulateDistrictEnergy.Instance.ResultsArray.NGAS_NGB.Max();
            CapShw = DHSimulateDistrictEnergy.Instance.ResultsArray.HW_SHW.Max();
            CapEch = DHSimulateDistrictEnergy.Instance.ResultsArray.CHW_ECH.Max();
            CapElecProj = DHSimulateDistrictEnergy.Instance.ResultsArray.ELEC_PROJ.Max();
            CapPv = DHSimulateDistrictEnergy.Instance.ResultsArray.ELEC_PV.Max();
            CapWnd = DHSimulateDistrictEnergy.Instance.ResultsArray.ELEC_WND.Max();
            AreaShw = DHSimulateDistrictEnergy.Instance.AllDistrictDemand.HW_n.Sum() * Settings.OFF_SHW /
                       (DHSimulateDistrictEnergy.Instance.AllDistrictDemand.RAD_n.Sum() * Settings.EFF_SHW * (1 - Settings.LOSS_SHW) * Settings.UTIL_SHW);
            AreaPv = DHSimulateDistrictEnergy.Instance.AllDistrictDemand.ELEC_n.Sum() * Settings.OFF_PV /
                      (DHSimulateDistrictEnergy.Instance.AllDistrictDemand.RAD_n.Sum() * Settings.EFF_PV * (1 - Settings.LOSS_PV) * Settings.UTIL_PV);
            var windCubed = DHSimulateDistrictEnergy.Instance.AllDistrictDemand.WIND_n.Where(w => w > Settings.CIN_WND && w < Settings.COUT_WND).Select(w => Math.Pow(w, 3))
                .Sum();
            NumWnd = Math.Ceiling(DHSimulateDistrictEnergy.Instance.AllDistrictDemand.ELEC_n.Sum() * Settings.OFF_WND /
                      (0.6375 * windCubed * Settings.ROT_WND * (1 - Settings.LOSS_WND) * Settings.COP_WND /
                       1000)); // Divide by 1000 because equation spits out Wh
            ChgrHwt = CapHwt == 0 ? 0 : CapHwt / 12 / Settings.AUT_HWT; // 12 hours // (AUT_HWT * 12);
            ChgrBat = CapBat == 0 ? 0 : CapBat / 12 / Settings.AUT_BAT;
            DchgrHwt = CapHwt == 0
                ? 0
                : CapHwt / 12 / Settings.AUT_HWT; // (AUT_HWT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
            DchgBat = CapBat == 0
                ? 0
                : CapBat / 12 / Settings.AUT_BAT; // (AUT_BAT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}