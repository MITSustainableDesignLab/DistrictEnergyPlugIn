using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using CsvHelper;
using DistrictEnergy.Helpers;
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
        public enum LoadTypes
        {
            Cooling,
            Heating,
            Elec
        }

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
            DistrictDemand = new DistrictDemand();
            SimConstants = new SimConstants();
            PluginSettings = new Settings();
        }

        public ResultsArray ResultsArray { get; }
        public DistrictDemand DistrictDemand { get; }
        public SimConstants SimConstants { get; }
        public Settings PluginSettings { get; }


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
                //if (i == 3473)
                //    Debugger.Break();
                eqHW_ABS(DistrictDemand.ChwN[i], out ResultsArray.HwAbs[i], out ResultsArray.ChwAbs[i],
                    ResultsArray.EhpEvap[i]); //OK
                eqELEC_ECH(DistrictDemand.ChwN[i], ResultsArray.EhpEvap[i], out ResultsArray.ElecEch[i],
                    out ResultsArray.ChwEch[i], out ResultsArray.ChwEhpEvap[i]); //OK
                eqHW_SHW(DistrictDemand.RadN[i], DistrictDemand.HwN[i], ResultsArray.HwAbs[i],
                    out ResultsArray.HwShw[i],
                    out ResultsArray.SHW_BAL[i]); // OK todo Surplus energy from the chp should go in the battery
                eqELEC_PV(DistrictDemand.RadN[i], out ResultsArray.ElecPv[i]); // OK
                eqELEC_WND(DistrictDemand.WindN[i], out ResultsArray.ElecWnd[i]); // OK
                if (i == 0) ResultsArray.TANK_CHG_n[i] = SimConstants.CapHwt * DistrictEnergy.Settings.TankStart;
                if (i > 0)
                    eqTANK_CHG_n(ResultsArray.TANK_CHG_n[i - 1], ResultsArray.SHW_BAL[i], DistrictDemand.TAmbN[i],
                        out ResultsArray.TANK_CHG_n[i]); // OK
                eqHW_HWT(ResultsArray.TANK_CHG_n[i], DistrictDemand.HwN[i], ResultsArray.HwAbs[i],
                    ResultsArray.HwShw[i], out ResultsArray.HwHwt[i]); // OK
                eqELEC_EHP(DistrictDemand.HwN[i], ResultsArray.HwAbs[i], ResultsArray.HwShw[i],
                    ResultsArray.HwHwt[i], ResultsArray.HwChp[i], out ResultsArray.ElecEhp[i],
                    out ResultsArray.HwEhp[i],
                    out ResultsArray.EhpEvap[i]); // Will call HW_CHP even though its not calculated yet

                // Call ABS & ECH a second time to calculate new Absoprtion & Electric chiller loads since heat pumps may have reduced the load
                eqHW_ABS(DistrictDemand.ChwN[i], out ResultsArray.HwAbs[i], out ResultsArray.ChwAbs[i],
                    ResultsArray.EhpEvap[i]);
                eqELEC_ECH(DistrictDemand.ChwN[i], ResultsArray.EhpEvap[i], out ResultsArray.ElecEch[i],
                    out ResultsArray.ChwEch[i], out ResultsArray.ChwEhpEvap[i]); //OK
                eqHW_SHW(DistrictDemand.RadN[i], DistrictDemand.HwN[i], ResultsArray.HwAbs[i],
                    out ResultsArray.HwShw[i],
                    out ResultsArray.SHW_BAL[i]); // OK todo Surplus energy from the chp should go in the battery
                eqELEC_PV(DistrictDemand.RadN[i], out ResultsArray.ElecPv[i]); // OK
                eqELEC_WND(DistrictDemand.WindN[i], out ResultsArray.ElecWnd[i]); // OK
                if (i == 0) ResultsArray.TANK_CHG_n[i] = SimConstants.CapHwt * DistrictEnergy.Settings.TankStart;
                if (i > 0)
                    eqTANK_CHG_n(ResultsArray.TANK_CHG_n[i - 1], ResultsArray.SHW_BAL[i], DistrictDemand.TAmbN[i],
                        out ResultsArray.TANK_CHG_n[i]); // OK
                eqHW_HWT(ResultsArray.TANK_CHG_n[i], DistrictDemand.HwN[i], ResultsArray.HwAbs[i],
                    ResultsArray.HwShw[i], out ResultsArray.HwHwt[i]); // OK

                // We continue...
                eqELEC_REN(ResultsArray.ElecPv[i], ResultsArray.ElecWnd[i], DistrictDemand.ElecN[i],
                    ResultsArray.ElecEch[i], ResultsArray.ElecEhp[i], out ResultsArray.ElecRen[i],
                    out ResultsArray.ElecBal[i]); // OK
                if (i == 0) ResultsArray.BatChgN[0] = SimConstants.CapBat * DistrictEnergy.Settings.BatStart;
                if (i > 0)
                    eqBAT_CHG_n(ResultsArray.BatChgN[i - 1], ResultsArray.ElecBal[i],
                        out ResultsArray.BatChgN[i]); // OK
                eqELEC_BAT(ResultsArray.BatChgN[i], DistrictDemand.ElecN[i], ResultsArray.ElecEch[i],
                    ResultsArray.ElecEhp[i], out ResultsArray.ElecBat[i], ResultsArray.ElecRen[i],
                    ResultsArray.ElecChp[i]); // OK

                if (string.Equals(DistrictEnergy.Settings.TmodChp, "Thermal"))
                {
                    // ignore NgasChp
                    eqHW_CHP(DistrictEnergy.Settings.TmodChp, DistrictDemand.HwN[i], ResultsArray.HwEhp[i],
                        ResultsArray.HwAbs[i],
                        ResultsArray.HwShw[i], ResultsArray.HwHwt[i], ResultsArray.NgasChp[i],
                        out ResultsArray.HwChp[i]);
                    // ignore Elec_Chp
                    eqNGAS_CHP(DistrictEnergy.Settings.TmodChp, ResultsArray.HwChp[i], ResultsArray.ElecChp[i],
                        out ResultsArray.NgasChp[i]);
                    // ignore ElecN, ElecRen, ElecBat
                    eqELEC_CHP(DistrictEnergy.Settings.TmodChp, ResultsArray.NgasChp[i], DistrictDemand.ElecN[i],
                        ResultsArray.ElecRen[i], ResultsArray.ElecBat[i], ResultsArray.ElecEch[i],
                        ResultsArray.ElecEhp[i],
                        out ResultsArray.ElecChp[i]);
                }

                if (string.Equals(DistrictEnergy.Settings.TmodChp, "Electrical"))
                {
                    // ignore NgasChp
                    eqELEC_CHP(DistrictEnergy.Settings.TmodChp, ResultsArray.NgasChp[i], DistrictDemand.ElecN[i],
                        ResultsArray.ElecRen[i], ResultsArray.ElecBat[i], ResultsArray.ElecEch[i],
                        ResultsArray.ElecEhp[i],
                        out ResultsArray.ElecChp[i]);
                    // ignore HwChp
                    eqNGAS_CHP(DistrictEnergy.Settings.TmodChp, ResultsArray.HwChp[i], ResultsArray.ElecChp[i],
                        out ResultsArray.NgasChp[i]);
                    // ignore HwN, HwEhp, HwAbs, HwShw, HwHwt
                    eqHW_CHP(DistrictEnergy.Settings.TmodChp, DistrictDemand.HwN[i], ResultsArray.HwEhp[i],
                        ResultsArray.HwAbs[i],
                        ResultsArray.HwShw[i], ResultsArray.HwHwt[i], ResultsArray.NgasChp[i],
                        out ResultsArray.HwChp[i]);
                }

                eqNGAS_NGB(DistrictDemand.HwN[i], ResultsArray.HwEhp[i], ResultsArray.HwAbs[i],
                    ResultsArray.HwShw[i], ResultsArray.HwHwt[i], ResultsArray.HwChp[i],
                    out ResultsArray.NgasNgb[i],
                    out ResultsArray.HwNgb[i]);
                eqELEC_proj(DistrictDemand.ElecN[i], ResultsArray.ElecRen[i], ResultsArray.ElecBat[i],
                    ResultsArray.ElecChp[i], ResultsArray.ElecEhp[i], ResultsArray.ElecEch[i],
                    out ResultsArray.ElecProj[i]);
                eqNGAS_proj(ResultsArray.NgasNgb[i], ResultsArray.NgasChp[i], out ResultsArray.NgasProj[i]);
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

            var _idList = new List<string>();
            foreach (var building in umiContext.Buildings.All) _idList.Add(building.Id.ToString());
            _progressBarPos = 0;
            // Getting the Aggregated Load Curve for all buildings
            var contextBuildings =
                umiContext.GetObjects()
                    .Where(o => o.Data.Any(x => x.Value.Data.Count == 8760) && _idList.Contains(o.Id)).ToList();
            if (contextBuildings.Count == 0)
            {
                MessageBox.Show(
                    "There are no buildings with hourly results. Please Rerun the Energy Module after turning on the Hourly Results in Advanced Options",
                    "Cannot continue with District simulation");
                return Result.Failure;
            }

            DistrictDemand.ChwN = GetHourlyChilledWaterProfile(contextBuildings);
            DistrictDemand.HwN = GetHourlyHotWaterLoadProfile(contextBuildings);
            DistrictDemand.ElecN = GetHourlyElectricalLoadProfile(contextBuildings);
            (DistrictDemand.AddC, DistrictDemand.AddH, DistrictDemand.AddE) = GetAdditionalLoadProfile(
                umiContext.GetObjects()
                    .Where(x => x.Name == "Additional Loads" && doc.Objects.FindId(Guid.Parse(x.Id)) != null).ToList());
            StatusBar.HideProgressMeter();
            DistrictDemand.RadN = GetHourlyLocationSolarRadiation(umiContext).ToArray();
            DistrictDemand.WindN = GetHourlyLocationWind(umiContext).ToArray();
            DistrictDemand.TAmbN = GetHourlyLocationAmbiantTemp(umiContext).ToArray();


            numberTimesteps = DistrictDemand.HwN.Length;

            RhinoApp.WriteLine(
                $"Calculated...\n{DistrictDemand.ChwN.Length} datapoints for ColdWater profile\n{DistrictDemand.HwN.Count()} datapoints for HotWater\n{DistrictDemand.ElecN.Count()} datapoints for Electricity\n{DistrictDemand.RadN.Count()} datapoints for Solar Frad\n{DistrictDemand.WindN.Count()} datapoints for WindSpeed");

            // Go Hour by hour and parse through the simulation routine
            SetResultsArraystoZero();
            DeleteLogFile();
            SimConstants.CalculateConstants();
            MainSimulation();
            //SimulationResultsToCsv();

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
                    if (DistrictEnergy.Settings.UseDistrictLosses)
                        // If distribution losses, increase demand

                    {
                        var lossChwnet = 1 + DistrictEnergy.Settings.LossChwnet;
                        DHSimulateDistrictEnergy.Instance.DistrictDemand.CoolingNetworkLosses[i] += d * lossChwnet;
                        d *= lossChwnet;
                    }

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

                    if (DistrictEnergy.Settings.UseDistrictLosses)
                    {
                        var lossHwnet = 1 + DistrictEnergy.Settings.LossHwnet;
                        DHSimulateDistrictEnergy.Instance.DistrictDemand.HeatingNetworkLosses[i] += d * lossHwnet;
                        d *= lossHwnet;
                    }

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

        /// <summary>
        ///     Adds the additional loads to DistrictDemand.
        /// </summary>
        /// <param name="additionalLoadObjects"></param>
        /// <returns>The additional loads</returns>
        private (double[], double[], double[]) GetAdditionalLoadProfile(List<UmiObject> additionalLoadObjects)
        {
            RhinoApp.WriteLine("Getting additional loads");
            var nbDataPoint = 8760;
            var addC = new double[nbDataPoint];
            var addH = new double[nbDataPoint];
            var addE = new double[nbDataPoint];
            StatusBar.HideProgressMeter();
            StatusBar.ShowProgressMeter(0, nbDataPoint * additionalLoadObjects.Count * 3,
                "Aggregating Hot Water Loads", true, true);
            foreach (var umiObject in additionalLoadObjects)
            foreach (var loadType in Enum.GetValues(typeof(LoadTypes)))
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var data = umiObject.Data[$"Additional {loadType} Load"].Data;
                    var d = data[i];
                    if (DistrictEnergy.Settings.UseDistrictLosses) d *= 1 + DistrictEnergy.Settings.LossHwnet;
                    // Add 
                    switch (loadType)
                    {
                        case LoadTypes.Cooling:
                            //Adding Additional loads
                            addC[i] += d;
                            DistrictDemand.ChwN[i] += d;
                            break;
                        case LoadTypes.Heating:
                            //Adding Additional loads
                            addH[i] += d;
                            DistrictDemand.HwN[i] += d;
                            break;
                        case LoadTypes.Elec:
                            //Adding Additional loads
                            addE[i] += d;
                            DistrictDemand.ElecN[i] += d;
                            break;
                    }

                    _progressBarPos += 1;
                    StatusBar.UpdateProgressMeter(_progressBarPos, true);
                }


            return (addC, addH, addE);
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
        ///     Equation 19 : Hourly purchased grid electricity for whole site
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
                    csvWriter.WriteField(ResultsArray.BatChgN[i]); // 3
                    csvWriter.WriteField(ResultsArray.ElecBat[i]); // 4
                    csvWriter.WriteField(ResultsArray.ElecChp[i]); // 5
                    csvWriter.WriteField(ResultsArray.ElecEch[i]); // 6
                    csvWriter.WriteField(ResultsArray.ElecEhp[i]); // 7
                    csvWriter.WriteField(ResultsArray.ElecPv[i]); // 8
                    csvWriter.WriteField(ResultsArray.ElecRen[i]); // 9
                    csvWriter.WriteField(ResultsArray.ElecWnd[i]); // 10
                    csvWriter.WriteField(ResultsArray.ElecBal[i]); // 11
                    csvWriter.WriteField(ResultsArray.HwAbs[i]); // 12
                    csvWriter.WriteField(ResultsArray.HwEhp[i]); // 13
                    csvWriter.WriteField(ResultsArray.HwChp[i]); // 14
                    csvWriter.WriteField(ResultsArray.HwHwt[i]); // 15
                    csvWriter.WriteField(ResultsArray.HwShw[i]); // 16
                    csvWriter.WriteField(ResultsArray.HwNgb[i]); // 17
                    csvWriter.WriteField(ResultsArray.NgasChp[i]); // 18
                    csvWriter.WriteField(ResultsArray.NgasNgb[i]); // 19
                    csvWriter.WriteField(ResultsArray.TANK_CHG_n[i]); // 20
                    csvWriter.WriteField(ResultsArray.SHW_BAL[i]); // 21
                    csvWriter.WriteField(ResultsArray.ElecProj[i]); // 22
                    csvWriter.WriteField(ResultsArray.NgasProj[i]); // 23
                    csvWriter.WriteField(DistrictDemand.ChwN[i]); // 24
                    csvWriter.WriteField(DistrictDemand.HwN[i]); // 25
                    csvWriter.WriteField(DistrictDemand.ElecN[i]); // 26
                    csvWriter.WriteField(ResultsArray.ChwAbs[i]); // 27
                    csvWriter.WriteField(ResultsArray.ChwEch[i]); // 28
                    csvWriter.WriteField(ResultsArray.ChwEhpEvap[i]); // 29

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

        #region Results Array

        private void SetResultsArraystoZero()
        {
            Array.Clear(ResultsArray.BatChgN, 0, ResultsArray.BatChgN.Length);
            Array.Clear(ResultsArray.ChwAbs, 0, ResultsArray.ChwAbs.Length);
            Array.Clear(ResultsArray.ChwEch, 0, ResultsArray.ChwEch.Length);
            Array.Clear(ResultsArray.ElecBat, 0, ResultsArray.ElecBat.Length);
            Array.Clear(ResultsArray.ElecChp, 0, ResultsArray.ElecChp.Length);
            Array.Clear(ResultsArray.ElecEch, 0, ResultsArray.ElecEch.Length);
            Array.Clear(ResultsArray.ElecEhp, 0, ResultsArray.ElecEhp.Length);
            Array.Clear(ResultsArray.ElecPv, 0, ResultsArray.ElecPv.Length);
            Array.Clear(ResultsArray.ElecRen, 0, ResultsArray.ElecRen.Length);
            Array.Clear(ResultsArray.ElecWnd, 0, ResultsArray.ElecWnd.Length);
            Array.Clear(ResultsArray.ElecBal, 0, ResultsArray.ElecBal.Length);
            Array.Clear(ResultsArray.HwAbs, 0, ResultsArray.HwAbs.Length);
            Array.Clear(ResultsArray.HwEhp, 0, ResultsArray.HwEhp.Length);
            Array.Clear(ResultsArray.HwChp, 0, ResultsArray.HwChp.Length);
            Array.Clear(ResultsArray.HwHwt, 0, ResultsArray.HwHwt.Length);
            Array.Clear(ResultsArray.HwShw, 0, ResultsArray.HwShw.Length);
            Array.Clear(ResultsArray.HwNgb, 0, ResultsArray.HwNgb.Length);
            Array.Clear(ResultsArray.NgasChp, 0, ResultsArray.NgasChp.Length);
            Array.Clear(ResultsArray.NgasNgb, 0, ResultsArray.NgasNgb.Length);
            Array.Clear(ResultsArray.TANK_CHG_n, 0, ResultsArray.TANK_CHG_n.Length);
            Array.Clear(ResultsArray.SHW_BAL, 0, ResultsArray.SHW_BAL.Length);
            Array.Clear(ResultsArray.ElecProj, 0, ResultsArray.ElecProj.Length);
            Array.Clear(ResultsArray.NgasProj, 0, ResultsArray.NgasProj.Length);
            Array.Clear(ResultsArray.EhpEvap, 0, ResultsArray.EhpEvap.Length);
            Array.Clear(ResultsArray.ChwEhpEvap, 0, ResultsArray.ChwEhpEvap.Length);
        }

        #endregion

        #region Equation 1 to 18

        /// <summary>
        ///     Equation 1 : The hot water required to generate project chilled water
        /// </summary>
        /// <param name="chwN">Hourly chilled water load profile (kWh)</param>
        /// <param name="hwAbs">Hot water required for Absorption Chiller</param>
        /// <param name="chwAbs"></param>
        /// <param name="ehpEvap"></param>
        private void eqHW_ABS(double chwN, out double hwAbs, out double chwAbs, double ehpEvap)
        {
            hwAbs = GetSmallestNonNegative(chwN - ehpEvap, SimConstants.CapAbs) / DistrictEnergy.Settings.CcopAbs;
            chwAbs = GetSmallestNonNegative(chwN - ehpEvap, SimConstants.CapAbs);
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
                elecEch = Math.Max(chwN - SimConstants.CapAbs - ehpEvap, 0) / DistrictEnergy.Settings.CcopEch;
                chwEch = Math.Max(chwN - SimConstants.CapAbs - ehpEvap, 0);

                if (ehpEvap > chwN - SimConstants.CapAbs)
                    chwEhpEvap = chwN - SimConstants.CapAbs;
                else
                    chwEhpEvap = ehpEvap;
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
            elecEhp = GetSmallestNonNegative(hwN + hwAbs - hwShw - hwHwt - hwChp, SimConstants.CapEhp) /
                      DistrictEnergy.Settings.HcopEhp;
            hwEhp = GetSmallestNonNegative(hwN + hwAbs - hwShw - hwHwt - hwChp, SimConstants.CapEhp);
            if (DistrictEnergy.Settings.UseEhpEvap)
                ehpEvap = hwEhp * (1 - 1 / DistrictEnergy.Settings.HcopEhp);
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
            ngasNgb = Math.Max(hwN - hwEhp + hwAbs - hwShw - hwHwt - hwChp, 0) /
                      DistrictEnergy.Settings.EffNgb; // If chp produces more energy than needed, ngas will be 0.
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
            hwShw = Math.Min(
                radN * SimConstants.AreaShw * DistrictEnergy.Settings.EffShw * DistrictEnergy.Settings.UtilShw *
                (1 - DistrictEnergy.Settings.LossShw), hwN + hwAbs);
            solarBalance =
                radN * SimConstants.AreaShw * DistrictEnergy.Settings.EffShw * DistrictEnergy.Settings.UtilShw *
                (1 - DistrictEnergy.Settings.LossShw) - hwN - hwAbs;
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
                tankChgN = Math.Max(previousTankChgN + shwBal,
                    GetHighestNonNegative(previousTankChgN - SimConstants.DchgrHwt, 0));
                SimConstants.LossHwt =
                    (-4E-5 * tAmb + 0.0024) * Math.Pow(tankChgN, -1 / 3); // todo tanklosses-to-tAmb relation
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
                           (1 - SimConstants
                               .LossHwt
                           ); // Contrary to the Grasshopper code, the tank loses energy to the environnement even when not used.
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
            hwHwt = GetSmallestNonNegative(hwN + hwAbs - hwShw,
                GetSmallestNonNegative(tankChgN, SimConstants.DchgrHwt));
        }

        /// <summary>
        ///     Equation 8 : The PV total electricity generation
        /// </summary>
        /// <param name="radN"></param>
        /// <param name="elecPv"></param>
        private void eqELEC_PV(double radN, out double elecPv)
        {
            elecPv = radN * SimConstants.AreaPv * DistrictEnergy.Settings.EffPv * DistrictEnergy.Settings.UtilPv *
                     (1 - DistrictEnergy.Settings.LossPv);
        }

        /// <summary>
        ///     Equation 9 : The annual electricity generation for Wind Turbines
        /// </summary>
        /// <param name="windN"></param>
        /// <param name="elecWnd"></param>
        private void eqELEC_WND(double windN, out double elecWnd)
        {
            elecWnd = 0.6375 * Math.Pow(windN, 3) * DistrictEnergy.Settings.RotWnd * SimConstants.NumWnd *
                      DistrictEnergy.Settings.CopWnd * (1 - DistrictEnergy.Settings.LossWnd) /
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
                batChgN = Math.Max(previousBatChgN + elecBalance * (1 - DistrictEnergy.Settings.LossBat),
                    GetHighestNonNegative(previousBatChgN - SimConstants.DchgBat, 0));
            else if (elecBalance > 0) // We are charging the battery
                batChgN = GetSmallestNonNegative(previousBatChgN + elecBalance * (1 - DistrictEnergy.Settings.LossBat),
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
                temp = GetSmallestNonNegative(
                    SimConstants.CapChpElec / DistrictEnergy.Settings.EffChp * DistrictEnergy.Settings.HrecChp,
                    hWn + hwAbs - hwShw - hwHwt - hwEhp); //hwN - hwEhp + hwAbs - hwShw - hwHwt
            if (string.Equals(tracking, "Electrical"))
                temp = ngasChp * DistrictEnergy.Settings.HrecChp;
            var demandToBeMetByChp = Math.Max(hWn + hwAbs - hwShw - hwHwt - hwEhp, 0);
            if (temp > demandToBeMetByChp) // CHP is forced to produce more energy than necessary
            {
                // Send Excess heat to Tank
                ResultsArray.SHW_BAL[i] = ResultsArray.SHW_BAL[i] + temp;
                if (i > 0)
                    eqTANK_CHG_n(ResultsArray.TANK_CHG_n[i - 1], ResultsArray.SHW_BAL[i], DistrictDemand.TAmbN[i],
                        out ResultsArray.TANK_CHG_n[i]);
                temp = demandToBeMetByChp;
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
                temp = hwChp / DistrictEnergy.Settings.HrecChp;
            if (string.Equals(tracking, "Electrical"))
                temp = elecChp / DistrictEnergy.Settings.EffChp;
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
                temp = ngasChp * DistrictEnergy.Settings.EffChp;
                if (temp > elecN + elecEhp + elecEch - elecRen - elecBat)
                {
                    // Send excess elec to Battery
                    ResultsArray.ElecBal[i] = ResultsArray.ElecBal[i] + temp;
                    if (i > 0)
                        eqBAT_CHG_n(ResultsArray.BatChgN[i - 1], ResultsArray.ElecBal[i],
                            out ResultsArray.BatChgN[i]);
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

        #region AvailableSettings

        #endregion
    }

    public class DistrictDemand
    {
        /// <summary>
        ///     Hourly Additional Cooling Load
        /// </summary>
        public double[] AddC = new double[8760];

        /// <summary>
        ///     Hourly Additional Electricity Load
        /// </summary>
        public double[] AddE = new double[8760];

        /// <summary>
        ///     Hourly Additional Heating Load
        /// </summary>
        public double[] AddH = new double[8760];

        /// <summary>
        ///     Hourly chilled water load profile (kWh)
        /// </summary>
        public double[] ChwN = new double[8760];

        /// <summary>
        ///     Hourly electricity load profile (kWh)
        /// </summary>
        public double[] ElecN = new double[8760];

        /// <summary>
        ///     Hourly hot water load profile (kWh)
        /// </summary>
        public double[] HwN = new double[8760];

        /// <summary>
        ///     Hourly Global Solar Radiation from EPW file (kWh/m2)
        /// </summary>
        public double[] RadN = new double[8760];

        /// <summary>
        ///     Hourly Ambiant temperature (C)
        /// </summary>
        public double[] TAmbN = new double[8760];

        /// <summary>
        ///     Hourly location wind speed data (m/s)
        /// </summary>
        public double[] WindN = new double[8760];

        /// <summary>
        ///     Hourly Losses through cooling network
        /// </summary>
        public double[] CoolingNetworkLosses = new double[8760];

        /// <summary>
        ///     Hourly Losses through heating network
        /// </summary>
        public double[] HeatingNetworkLosses = new double[8760];

    }

    public class ResultsArray
    {
        /// <summary>
        ///     eq11 The battery charge for each hour
        /// </summary>
        internal readonly double[] BatChgN = new double[8760];

        /// <summary>
        ///     Chilled Water met by Absorption Chiller
        /// </summary>
        internal readonly double[] ChwAbs = new double[8760];

        /// <summary>
        ///     Chilled Water met by Electric Chillers
        /// </summary>
        internal readonly double[] ChwEch = new double[8760];

        /// <summary>
        /// </summary>
        internal readonly double[] ChwEhpEvap = new double[8760];

        /// <summary>
        ///     Energy recovered from Electrical Heat Pumps on the evaporator side
        /// </summary>
        internal readonly double[] EhpEvap = new double[8760];

        /// <summary>
        ///     Electricity generation balance from renewables only
        /// </summary>
        internal readonly double[] ElecBal = new double[8760];

        /// <summary>
        ///     eq12 Demand met by the battery bank
        /// </summary>
        internal readonly double[] ElecBat = new double[8760];

        /// <summary>
        ///     eq15/16 Electricity generated by CHP plant
        /// </summary>
        internal readonly double[] ElecChp = new double[8760];

        /// <summary>
        ///     eq2 Electricity Consumption to generate chilled water from chillers
        /// </summary>
        internal readonly double[] ElecEch = new double[8760];

        /// <summary>
        ///     eq3 Electricity consumption required to generate hot water from HPs
        /// </summary>
        internal readonly double[] ElecEhp = new double[8760];

        /// <summary>
        ///     Hourly purchased grid electricity
        /// </summary>
        internal readonly double[] ElecProj = new double[8760];

        /// <summary>
        ///     eq8 Total PV electricity generation
        /// </summary>
        internal readonly double[] ElecPv = new double[8760];

        /// <summary>
        ///     eq10 Total nenewable electricity generation
        /// </summary>
        internal readonly double[] ElecRen = new double[8760];

        /// <summary>
        ///     eq9 Total Wind electricity generation
        /// </summary>
        internal readonly double[] ElecWnd = new double[8760];

        /// <summary>
        ///     eq1 Hot water required for Absorption Chiller
        /// </summary>
        internal readonly double[] HwAbs = new double[8760];

        /// <summary>
        ///     eq13 Heating energy recovered from the combined heat and power plant and supplied to the project
        /// </summary>
        internal readonly double[] HwChp = new double[8760];

        /// <summary>
        ///     Hot water met by electric heat pumps
        /// </summary>
        internal readonly double[] HwEhp = new double[8760];

        /// <summary>
        ///     eq7 Demand met by hot water tanks
        /// </summary>
        internal readonly double[] HwHwt = new double[8760];

        /// <summary>
        ///     Hot Water produced by Natural Gas Boilers
        /// </summary>
        internal readonly double[] HwNgb = new double[8760];

        /// <summary>
        ///     eq5 Total Solar Hot Water generation to meet building loads
        /// </summary>
        internal readonly double[] HwShw = new double[8760];

        /// <summary>
        ///     eq14/17 Natural gas consumed by CHP plant
        /// </summary>
        internal readonly double[] NgasChp = new double[8760];

        /// <summary>
        ///     eq4 Boiler natural gas consumption to generate project hot water
        /// </summary>
        internal readonly double[] NgasNgb = new double[8760];

        /// <summary>
        ///     Hourly purchased natural gas
        /// </summary>
        internal readonly double[] NgasProj = new double[8760];

        /// <summary>
        ///     Hour Surplus
        /// </summary>
        internal readonly double[] SHW_BAL = new double[8760];

        /// <summary>
        ///     eq6 The tank charge for each hour [kWh]
        /// </summary>
        internal readonly double[] TANK_CHG_n = new double[8760];


        public event EventHandler ResultsChanged;


        protected internal virtual void OnResultsChanged(EventArgs e)
        {
            var handler = ResultsChanged;
            handler?.Invoke(this, e);
        }
    }

    /// <summary>
    ///     Properties of energy sources
    /// </summary>
    public class Settings : INotifyPropertyChanged
    {
        private TimeGroupers _aggregationPeriod = TimeGroupers.Monthly;

        public TimeGroupers AggregationPeriod
        {
            get => _aggregationPeriod;
            set
            {
                _aggregationPeriod = value;
                OnPropertyChanged(nameof(AggregationPeriod));
            }
        }

        /// <summary>
        ///     Cooling coefficient of performance: Average annual ratio of useful cooling delivered to electricity consumed
        /// </summary>
        public static double CcopEch
        {
            get { return PlantSettingsViewModel.Instance.CCOP_ECH; }
        }

        /// <summary>
        ///     Heating efficiency (%) : Average annual ratio of useful heating delivered to fuel consumed
        /// </summary>
        public static double EffNgb
        {
            get { return PlantSettingsViewModel.Instance.EFF_NGB / 100; }
        }

        /// <summary>
        ///     Capacity as percent of peak cooling load (%)
        /// </summary>
        internal static double OffAbs
        {
            get { return PlantSettingsViewModel.Instance.OFF_ABS / 100; }
        }

        /// <summary>
        ///     Cooling coefficient of performance
        /// </summary>
        public static double CcopAbs
        {
            get { return PlantSettingsViewModel.Instance.CCOP_ABS; }
        }

        /// <summary>
        ///     Capacity as number of days of autonomy (#)
        /// </summary>
        internal static double AutBat
        {
            get { return PlantSettingsViewModel.Instance.AUT_BAT; }
        }

        /// <summary>
        ///     Miscellaneous losses (%). Accounts for other losses including line losses and balance of system.
        /// </summary>
        public static double LossBat
        {
            get { return PlantSettingsViewModel.Instance.LOSS_BAT / 100; }
        }

        /// <summary>
        ///     Tracking mode. Control the generator to prioritize meeting the hot water or electricity demand.
        /// </summary>
        public static string TmodChp
        {
            get { return PlantSettingsViewModel.Instance.TMOD_CHP.ToString(); }
        }

        /// <summary>
        ///     Capacity as percent of peak electric load (%).
        /// </summary>
        internal static double OffChp
        {
            get { return PlantSettingsViewModel.Instance.OFF_CHP / 100; }
        }

        /// <summary>
        ///     Electrical efficiency (%).
        /// </summary>
        public static double EffChp
        {
            get { return PlantSettingsViewModel.Instance.EFF_CHP / 100; }
        }

        /// <summary>
        ///     Waste heat recovery effectiveness (%).
        /// </summary>
        public static double HrecChp
        {
            get { return PlantSettingsViewModel.Instance.HREC_CHP / 100; }
        }

        /// <summary>
        ///     Capacity as percent of peak heating load (%).
        /// </summary>
        internal static double OffEhp
        {
            get { return PlantSettingsViewModel.Instance.OFF_EHP / 100; }
        }

        /// <summary>
        ///     Heating coefficient of performance.
        /// </summary>
        public static double HcopEhp
        {
            get { return PlantSettingsViewModel.Instance.HCOP_EHP; }
        }

        /// <summary>
        ///     Capacity as the number of days of autonomy (#).
        /// </summary>
        internal static double AutHwt
        {
            get { return PlantSettingsViewModel.Instance.AUT_HWT; }
        }

        /// <summary>
        ///     Target offset as percent of annual energy (%).
        /// </summary>
        internal static double OffPv
        {
            get { return PlantSettingsViewModel.Instance.OFF_PV / 100; }
        }

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        public static double UtilPv
        {
            get { return PlantSettingsViewModel.Instance.UTIL_PV / 100; }
        }

        /// <summary>
        ///     Miscellaneous losses (%).
        /// </summary>
        public static double LossPv
        {
            get { return PlantSettingsViewModel.Instance.LOSS_PV / 100; }
        }

        /// <summary>
        ///     Cell efficiency (%).
        /// </summary>
        public static double EffPv
        {
            get { return PlantSettingsViewModel.Instance.EFF_PV / 100; }
        }

        /// <summary>
        ///     Collector efficiency (%)
        /// </summary>
        public static double EffShw
        {
            get { return PlantSettingsViewModel.Instance.EFF_SHW / 100; }
        }

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        public static double LossShw
        {
            get { return PlantSettingsViewModel.Instance.LOSS_SHW / 100; }
        }

        /// <summary>
        ///     Target offset as percent of annual energy (%)
        /// </summary>
        internal static double OffShw
        {
            get { return PlantSettingsViewModel.Instance.OFF_SHW / 100; }
        }

        /// <summary>
        ///     Area utilization factor (%)
        /// </summary>
        public static double UtilShw
        {
            get { return PlantSettingsViewModel.Instance.UTIL_SHW / 100; }
        }

        /// <summary>
        ///     Cut-in speed (m/s)
        /// </summary>
        internal static double CinWnd
        {
            get { return PlantSettingsViewModel.Instance.CIN_WND; }
        }

        /// <summary>
        ///     Turbine coefficient of performance
        /// </summary>
        public static double CopWnd
        {
            get { return PlantSettingsViewModel.Instance.EFF_WND / 100; }
        }

        /// <summary>
        ///     Cut-out speed (m/s)
        /// </summary>
        internal static double CoutWnd
        {
            get { return PlantSettingsViewModel.Instance.COUT_WND; }
        }

        /// <summary>
        ///     Target offset as percent of annual energy (%).
        /// </summary>
        internal static double OffWnd
        {
            get { return PlantSettingsViewModel.Instance.OFF_WND / 100; }
        }

        /// <summary>
        ///     Rotor area per turbine (m2)
        /// </summary>
        public static double RotWnd
        {
            get { return PlantSettingsViewModel.Instance.ROT_WND; }
        }

        /// <summary>
        ///     Miscellaneous losses (%)
        /// </summary>
        public static double LossWnd
        {
            get { return PlantSettingsViewModel.Instance.LOSS_WND / 100; }
        }

        /// <summary>
        ///     The Tank charged state at the begining of the simulation
        /// </summary>
        public static double TankStart
        {
            get { return PlantSettingsViewModel.Instance.TANK_START / 100; }
        }

        /// <summary>
        ///     The Battery charged state at the begining of the simulation
        /// </summary>
        public static double BatStart
        {
            get { return PlantSettingsViewModel.Instance.BAT_START / 100; }
        }

        public static double LossHwnet
        {
            get { return PlantSettingsViewModel.Instance.RelDistHeatLoss / 100; }
        }

        /// <summary>
        /// </summary>
        public static double LossChwnet
        {
            get { return PlantSettingsViewModel.Instance.RelDistCoolLoss / 100; }
        }

        /// <summary>
        /// </summary>
        public static bool UseDistrictLosses
        {
            get { return PlantSettingsViewModel.Instance.UseDistrictLosses; }
        }

        public static bool UseEhpEvap
        {
            get { return PlantSettingsViewModel.Instance.UseEhpEvap; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SimConstants : INotifyPropertyChanged
    {
        private double _areaPv;
        private double _areaShw;
        private double _capAbs;
        private double _capBat;
        private double _capChpElec;
        private double _capChpHeat;
        private double _capEch;
        private double _capEhp;
        private double _capElecProj;
        private double _capHwt;
        private double _capNgb;
        private double _capPv;
        private double _capShw;
        private double _capWnd;
        private double _chgrBat;
        private double _chgrHwt;
        private double _dchgBat;
        private double _dchgrHwt;
        private double _lossHwt;
        private double _numWnd;
        private double _relDistCoolLoss;
        private double _relDistHeatLoss;

        public SimConstants()
        {
            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
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

        public double RelDistHeatLoss
        {
            get { return _relDistHeatLoss; }
            set
            {
                _relDistHeatLoss = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_relDistHeatLoss)));
            }
        }

        public double RelDistCoolLoss
        {
            get { return _relDistCoolLoss; }
            set
            {
                _relDistHeatLoss = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_relDistCoolLoss)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
        ///     Calculates the necessary constants used in different equations
        /// </summary>
        public void CalculateConstants()
        {
            CapAbs = DHSimulateDistrictEnergy.Instance.DistrictDemand.ChwN.Max() * Settings.OffAbs;
            CapEhp = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Max() * Settings.OffEhp;
            CapBat = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Average() * Settings.AutBat * 24;
            CapHwt = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Average() * Settings.AutHwt *
                     24; // todo Prendre jour moyen du mois max.
            CapChpElec = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Max() * Settings.OffChp;
            CapChpHeat = DHSimulateDistrictEnergy.Instance.ResultsArray.HwChp.Max();
            CapNgb = DHSimulateDistrictEnergy.Instance.ResultsArray.NgasNgb.Max();
            CapShw = DHSimulateDistrictEnergy.Instance.ResultsArray.HwShw.Max();
            CapEch = DHSimulateDistrictEnergy.Instance.ResultsArray.ChwEch.Max();
            CapElecProj = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecProj.Max();
            CapPv = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecPv.Max();
            CapWnd = DHSimulateDistrictEnergy.Instance.ResultsArray.ElecWnd.Max();
            AreaShw = DHSimulateDistrictEnergy.Instance.DistrictDemand.HwN.Sum() * Settings.OffShw /
                      (DHSimulateDistrictEnergy.Instance.DistrictDemand.RadN.Sum() * Settings.EffShw *
                       (1 - Settings.LossShw) * Settings.UtilShw);
            AreaPv = DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Sum() * Settings.OffPv /
                     (DHSimulateDistrictEnergy.Instance.DistrictDemand.RadN.Sum() * Settings.EffPv *
                      (1 - Settings.LossPv) * Settings.UtilPv);
            var windCubed = DHSimulateDistrictEnergy.Instance.DistrictDemand.WindN
                .Where(w => w > Settings.CinWnd && w < Settings.CoutWnd).Select(w => Math.Pow(w, 3))
                .Sum();
            NumWnd = Math.Ceiling(DHSimulateDistrictEnergy.Instance.DistrictDemand.ElecN.Sum() * Settings.OffWnd /
                                  (0.6375 * windCubed * Settings.RotWnd * (1 - Settings.LossWnd) * Settings.CopWnd /
                                   1000)); // Divide by 1000 because equation spits out Wh
            ChgrHwt = CapHwt == 0 ? 0 : CapHwt / 12 / Settings.AutHwt; // 12 hours // (AUT_HWT * 12);
            ChgrBat = CapBat == 0 ? 0 : CapBat / 12 / Settings.AutBat;
            DchgrHwt = CapHwt == 0
                ? 0
                : CapHwt / 12 /
                  Settings.AutHwt; // (AUT_HWT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
            DchgBat = CapBat == 0
                ? 0
                : CapBat / 12 /
                  Settings.AutBat; // (AUT_BAT * 24); // todo Discharge rate is set to Capacity divided by desired nb of days of autonomy
        }
    }

    public class SimCase
    {
        private int _id;

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}