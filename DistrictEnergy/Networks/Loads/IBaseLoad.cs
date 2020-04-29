using System.Collections.Generic;
using DistrictEnergy.Helpers;
using LiveCharts.Defaults;

namespace DistrictEnergy.Networks.Loads
{
    internal interface IBaseLoad
    {
        List<DateTimePoint> Input { get; set; }
        LoadTypes LoadType { get; set; }
    }
}