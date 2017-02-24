using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;

namespace DistrictEnergy
{
    [System.Runtime.InteropServices.Guid("817ab30a-d9ba-4b0a-9a10-d8ad99063ed2")]
    public class ReverseCurve : Command
    {
        static ReverseCurve _instance;
        public ReverseCurve()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ReverseCurve command.</summary>
        public static ReverseCurve Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "ReverseCurve"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            ObjRef[] obj_refs;
            var rc = RhinoGet.GetMultipleObjects("Select curves to reverse", true, ObjectType.Curve, out obj_refs);
            if (rc != Result.Success)
                return rc;

            foreach (var obj_ref in obj_refs)
            {
                var curve_copy = obj_ref.Curve().DuplicateCurve();
                if (curve_copy != null)
                {
                    curve_copy.Reverse();
                    doc.Objects.Replace(obj_ref, curve_copy);
                }
            }
            return Result.Success;
        }
    }
}
