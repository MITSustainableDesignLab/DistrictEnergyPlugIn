using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using System.Collections.Generic;
using Rhino.Geometry;

namespace DistrictEnergy
{
    [System.Runtime.InteropServices.Guid("370cb7de-ff8b-4c15-8dfd-d9408de251c8")]
    public class ConnectBldgsToPlant : Command

    {
        static ConnectBldgsToPlant _instance;
        public ConnectBldgsToPlant()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ConnectBldgsToPlant command.</summary>
        public static ConnectBldgsToPlant Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "ConnectBldgsToPlant"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Select houses
            ObjRef[] buildingRefs;
            Result rc0 = Rhino.Input.RhinoGet.GetMultipleObjects("Select Buildings to be connected",true, ObjectType.Brep,out buildingRefs);
            if (rc0 != Rhino.Commands.Result.Success)
                return rc0;
            if (buildingRefs == null || buildingRefs.Length < 1)
                return Rhino.Commands.Result.Failure;

            List<Rhino.Geometry.Brep> in_breps0 = new List<Rhino.Geometry.Brep>();
            for (int i = 0; i < buildingRefs.Length; i++)
            {
                Rhino.Geometry.Brep brep = buildingRefs[i].Brep();
                if (brep != null)
                    in_breps0.Add(brep);
            }

            doc.Objects.UnselectAll();

            // Select 1 Thermal Plant
            ObjRef[] plantRefs;
            Result rc1 = RhinoGet.GetMultipleObjects("Select 1 Thermal Plant", true, ObjectType.Brep, out plantRefs);
            if (rc1 != Result.Success)
                return rc1;
            if (plantRefs == null || plantRefs.Length < 1)
                return Result.Failure;

            List<Rhino.Geometry.Brep> in_breps1 = new List<Rhino.Geometry.Brep>();
            for (int i = 0; i < plantRefs.Length; i++)
            {
                Rhino.Geometry.Brep brep = plantRefs[i].Brep();
                if (brep != null)
                    in_breps1.Add(brep);
            }

            doc.Objects.UnselectAll();

            RhinoApp.WriteLine("{0} buildings and {1} thermal plant added", in_breps0.Count, in_breps1.Count);

            // Create lines between all buildings and thermal plant

            List<double> XcoordinatesBuildings = new List<double>();
            List<double> YcoordinatesBuildings = new List<double>();
            for (int i = 0; i < in_breps0.Count; i++)
            {
                for (int j = 0; i < in_breps0.Count; i++)
                {
                    VolumeMassProperties vmp0 = VolumeMassProperties.Compute(buildingRefs[i].Brep());
                    VolumeMassProperties vmp1 = VolumeMassProperties.Compute(plantRefs[j].Brep());
                    //XcoordinatesBuildings[i] = vmp0.Centroid.X;
                    //YcoordinatesBuildings[i] = vmp0.Centroid.Y;

                    Point3d pt_start = new Point3d(vmp0.Centroid.X, vmp0.Centroid.Y, 0);
                    Point3d pt_end = new Point3d(vmp1.Centroid.X, vmp1.Centroid.Y, 0);
                    Vector3d v = new Vector3d(pt_end - pt_start);
                    if (v.IsTiny(Rhino.RhinoMath.ZeroTolerance))
                        return Rhino.Commands.Result.Nothing;

                    if (doc.Objects.AddLine(pt_start,pt_end) != Guid.Empty)
                    {
                        doc.Views.Redraw();
                        //return Rhino.Commands.Result.Success;
                    }
                }  
            }

            return Result.Success;
        }
    }
}
