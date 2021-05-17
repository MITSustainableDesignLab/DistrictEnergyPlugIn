using System.Collections.Generic;
using System.Linq;
using EnergyPlusWeather;
using Rhino;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class SolarInput : NotStorage
    {
        public static IEnumerable<double> SolarNormalRadiation { get; set; }

        /// <summary>
        ///     Maximum area of solar panel specified by the user [m2].
        /// </summary>
        public abstract double RequiredArea { get; set; }

        /// <summary>
        ///     If True, the supply unit capacity is constrained by <Property>UserMaxArea</Property>
        /// </summary>
        public abstract bool IsForcedDimensionCapacity { get; set; }

        public abstract double[] SolarAvailableInput(int t = 0, int dt = 8760);


        public static void GetHourlyLocationSolarRadiation(UmiContext context)
        {
            RhinoApp.WriteLine("Calculating Solar Radiation on horizontal surfaces...");
            var a = new EPWeatherData();
            a.GetRawData(context.WeatherFilePath);
            var radiation = a.HourlyWeatherDataRawList.Select(b => (double) b.GHorRadiation / 1000.0);
            RhinoApp.WriteLine("Completed Solar Radiation");
            SolarNormalRadiation = radiation;
            // return radiation; // from Wh to kWh
        }
    }
}