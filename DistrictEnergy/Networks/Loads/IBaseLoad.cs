using System.Collections.Generic;
using System.Windows.Media;
using DistrictEnergy.Helpers;
using Umi.Core;
using Umi.RhinoServices.Context;

namespace DistrictEnergy.Networks.Loads
{
    public interface IBaseLoad
    {
        double[] Input { get; set; }
        LoadTypes LoadType { get; set; }
        string Name { get; set; }
        SolidColorBrush Fill { get; set; }
        void GetUmiLoads(List<UmiObject> contextBuilding, UmiContext umiContext);
    }
}