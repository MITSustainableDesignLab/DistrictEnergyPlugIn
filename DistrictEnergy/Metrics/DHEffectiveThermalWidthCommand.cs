using Rhino;
using Rhino.Commands;

namespace DistrictEnergy.Metrics
{
    [System.Runtime.InteropServices.Guid("17bca1ed-43ed-4aed-8f18-9777744185f7")]
    public class DHEffectiveThermalWidthCommand : Command
    {
        static DHEffectiveThermalWidthCommand _instance;
        public DHEffectiveThermalWidthCommand()
        {
            _instance = this;
        }

        ///<summary>In order to further distinguish suitable areas for district heating, 
        ///the concept of thermal width was introduced by Sven Werner (Werner, 1997).
        ///Depending on the network-design, an area with a certain thermal density can have 
        ///different thermal length and thermal width</summary>
        public static DHEffectiveThermalWidthCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DHEffectiveThermalWidth"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            #region 1

            #endregion

            double length = Metrics.TotalRouteLength();
            double area = Metrics.LandArea();
            double thermalWidth = new double();

            if (length != -1 && area != -1)
            {
                thermalWidth = Metrics.EffThermalWidth(area, length);
            }

            RhinoApp.WriteLine("Land area: {0:F0}", area);
            RhinoApp.WriteLine("Total route length: {0:F1}", length);
            RhinoApp.WriteLine("Effective width: {0:F2} [m]", thermalWidth);

            return Result.Success;
        }
    }
}
