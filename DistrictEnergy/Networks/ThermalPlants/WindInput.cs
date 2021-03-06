﻿using System.Collections.Generic;
using System.Linq;
using EnergyPlusWeather;
using Rhino;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class WindInput : NotStorage
    {
        public static IEnumerable<double> WindSpeed { get; set; }

        /// <summary>
        ///     If True, the supply unit capacity is constrained by <Property>UserMaxNumberOfWindTurbines</Property>
        /// </summary>
        public bool IsForcedDimensionCapacity { get; set; }

        public abstract double RequiredNumberOfWindTurbines { get; set; }

        public abstract List<double> WindAvailableInput(int t = 0, int dt = 8760);
        public abstract List<double> PowerPerTurbine(int t = 0, int dt = 8760);

        public static void GetHourlyLocationWind(UmiContext context)
        {
            RhinoApp.WriteLine("Calculating wind for location...");
            var a = new EPWeatherData();
            a.GetRawData(context.WeatherFilePath);
            var wind = a.HourlyWeatherDataRawList.Select(b => (double) b.WindSpeed);
            RhinoApp.WriteLine("Completed wind");
            WindSpeed = wind;
        }
    }
}