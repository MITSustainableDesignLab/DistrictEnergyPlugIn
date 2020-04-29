using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;

namespace DistrictEnergy.Deprecated_Commands
{
    [System.Runtime.InteropServices.Guid("0c203e26-7023-4f60-ac69-b6339d7547e9")]
    public class CalculateCentroidXYZ : Command
    {
        static CalculateCentroidXYZ _instance;
        public CalculateCentroidXYZ()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CalculateCentroidXYZ command.</summary>
        public static CalculateCentroidXYZ Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "CalculateCentroidXYZ"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            ObjRef obj_ref;
            var rc = RhinoGet.GetOneObject("Select brep", true, ObjectType.Brep, out obj_ref);
            if (rc != Result.Success)
                return rc;
            VolumeMassProperties vmp = VolumeMassProperties.Compute(obj_ref.Brep());

            var X = vmp.Centroid.X;
            var Y = vmp.Centroid.Y;

            RhinoApp.WriteLine(string.Format("Brep is located at {0},{1}", X,Y));

            return Result.Success;
        }
    }
}
