﻿using System;
using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts.Defaults;
using Rhino;
using Umi.Core;

namespace DistrictEnergy.Networks.Loads
{
    class ElectricityLoads : BaseLoad
    {
        public ElectricityLoads()
        {

        }

        public override Guid Id { get; set; } = Guid.NewGuid();
        public override LoadTypes LoadType { get; set; } = LoadTypes.Elec;
        public override SolidColorBrush Fill { get; set; } = new SolidColorBrush(Color.FromRgb(173, 221, 67));
        public override string Name { get; set; } = "Electricity Load";
        public override void GetUmiLoads(List<UmiObject> contextObjects)
        {
            RhinoApp.WriteLine("Getting all Buildings and aggregating electrical loads: SDL/Equipment + SDL/Lighting");
            var nbDataPoint = 8760;
            var aggreagationArray = new double[nbDataPoint];
            foreach (var umiObject in contextObjects)
                for (var i = 0; i < nbDataPoint; i++)
                {
                    var d = umiObject.Data["SDL/Equipment"].Data[i] + umiObject.Data["SDL/Lighting"].Data[i];
                    aggreagationArray[i] += d;
                }

            Input = aggreagationArray;
        }
        /// <summary>
        /// Equipment & Lighting
        /// </summary>
        public override double[] Input { get; set; }
    }
}