using System;
using Rhino;
using Rhino.Commands;
using System.Linq;
using Rhino.Geometry;

namespace DistrictEnergy.Metrics
{
    [System.Runtime.InteropServices.Guid("17bca1ed-43ed-4aed-8f18-9777744185f7")]
    public class EffectiveThermalWidthCommand : Command
    {
        static EffectiveThermalWidthCommand _instance;
        public EffectiveThermalWidthCommand()
        {
            _instance = this;
        }

        ///<summary>In order to further distinguish suitable areas for district heating, 
        ///the concept of thermal width was introduced by Sven Werner (Werner, 1997).
        ///Depending on the network-design, an area with a certain thermal density can have 
        ///different thermal length and thermal width</summary>
        public static EffectiveThermalWidthCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "EffectiveThermalWidth"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            #region 1
            string layername = "Heating Network";

            // Get all of the objects on the layer. If layername is bogus, you will
            // just get an empty list back
            Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(layername).Where(x => x.ObjectType == Rhino.DocObjects.ObjectType.Curve).ToArray();
            if (rhobjs == null || rhobjs.Length < 1)
                return Rhino.Commands.Result.Cancel;

            var curves = new Curve[rhobjs.Length];

            for (int i = 0; i < rhobjs.Length; i++)
            {
                GeometryBase geom = rhobjs[i].Geometry;
                Curve x = geom as Curve;
                if (x != null && x.IsValid)
                {
                    curves[i] = x;
                }
            }
            double lenght = curves.Sum(x => x.GetLength());
            #endregion

            string layername1 = "Site boundary";

            // Get all of the objects on the layer. If layername is bogus, you will
            // just get an empty list back
            Rhino.DocObjects.RhinoObject[] rhobjs1 = doc.Objects.FindByLayer(layername1).Where(x => x.ObjectType == Rhino.DocObjects.ObjectType.Curve).ToArray();
            if (rhobjs1 == null || rhobjs1.Length < 1)
            {
                RhinoApp.WriteLine("Error: No boundary in project found");
                return Rhino.Commands.Result.Failure;
            }

            var curves1 = new Curve[rhobjs1.Length];

            for (int i = 0; i < rhobjs1.Length; i++)
            {
                GeometryBase geom = rhobjs1[i].Geometry;
                Curve x = geom as Curve;
                if (x != null && x.IsValid)
                {
                    curves1[i] = x;
                }
            }
            double area = new double();
            foreach (var o in curves1)
            {
                var areaMassP = AreaMassProperties.Compute(o);
                if (areaMassP != null)
                    area = area + areaMassP.Area;
            }

            var thermalWidth = Metrics.EffThermalWidth(area, lenght);

            RhinoApp.WriteLine("Land area: {0:F0}", area);
            RhinoApp.WriteLine("Total route length: {0:F1}", lenght);
            RhinoApp.WriteLine("Effective width: {0:F2} [m]", thermalWidth);

            return Result.Success;
        }
    }
}
