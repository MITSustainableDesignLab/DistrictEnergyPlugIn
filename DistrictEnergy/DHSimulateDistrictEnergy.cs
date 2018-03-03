using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EnergyPlusWeather;
using Mit.Umi.RhinoServices.Context;
using Rhino;
using Rhino.Commands;

namespace DistrictEnergy
{
    [Guid("929185AA-DB2C-4AA5-B1C0-E89C93F0704D")]
    public class DHSimulateDistrictEnergy : Command
    {
        public DHSimulateDistrictEnergy()
        {
            Instance = this;
        }

        ///<summary>The only instance of the DHSimulateDistrictEnergy command.</summary>
        public static DHSimulateDistrictEnergy Instance { get; private set; }

        public override string EnglishName => "DHSimulateDistrictEnergy";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var umiContext = UmiContext.Current;
            if (umiContext == null)
            {
                RhinoApp.WriteLine("Problem getting the umi context");
                return Result.Failure;
            }

            // Getting the Load Curve for all buildings

            var CHW_n = GetHourlyChilledWaterProfile(umiContext);
            var HW_n = GetHourlyHotWaterLoadProfile(umiContext);
            var ELEC_n = GetHourlyElectricalLoadProfile(umiContext);
            var RAD_n = GetHourlyLocationSolarRadiation(umiContext);
            var WIND_n = GetHourlyLocationWind(umiContext);

            RhinoApp.WriteLine(
                $"Calculated...\n{CHW_n.Count()} datapoints for ColdWater profile\n{HW_n.Count()} datapoints for HotWater\n{ELEC_n.Count()} datapoints for Electricity\n{RAD_n.Count()} datapoints for Solar Frad\n{WIND_n.Count()} datapoints for WindSpeed");

            return Result.Success;
        }

        private IEnumerable<double> GetHourlyChilledWaterProfile(UmiContext context)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating cooling loads");
            var nbDataPoint = context.GetObjects().Select(b => b.Data["SDL/Cooling"].Data.Count).ToList()[0];
            return Enumerable.Range(0, nbDataPoint)
                .Select(i => context.GetObjects().Select(
                    b => b.Data["SDL/Cooling"].Data[i]
                ).Sum());
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
    }
}