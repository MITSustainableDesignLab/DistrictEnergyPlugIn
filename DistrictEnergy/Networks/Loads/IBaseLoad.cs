using System.Collections.Generic;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.Loads
{
    internal interface IBaseLoad
    {
        double[] Input { get; set; }
        LoadTypes LoadType { get; set; }
    }
}