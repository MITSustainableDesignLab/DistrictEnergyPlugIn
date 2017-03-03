using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using System.Collections.Generic;
using ln = MathNet.Numerics.LinearAlgebra;

namespace DistrictEnergy
{
    [System.Runtime.InteropServices.Guid("a71b4494-268a-445c-be88-df521ac9268b")]
    public class ConnectitityMatrix : Command
    {
        static ConnectitityMatrix _instance;
        public ConnectitityMatrix()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ConnectitityMatrix command.</summary>
        public static ConnectitityMatrix Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "ConnectitityMatrix"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Loading object curves and creating start and endpoints
            ObjRef[] obj_ref;
            var rc = RhinoGet.GetMultipleObjects("Select Network curves", false, ObjectType.Curve, out obj_ref);
                
            if (rc != Result.Success)
                 return rc;
            if (obj_ref == null)
                return Result.Nothing;
            List<Rhino.Geometry.Point3d> listPts = new List<Rhino.Geometry.Point3d>();
            List<Rhino.Geometry.Curve> listCrv = new List<Rhino.Geometry.Curve>();
            foreach (var c in obj_ref)
            {
                var curve = c.Curve();
                listCrv.Add(curve);
                var startPt = curve.PointAtStart;
                var endPt = curve.PointAtEnd;
                if (listPts.Contains(startPt) != true)
                    listPts.Add(startPt);
                if (listPts.Contains(endPt) != true)
                    listPts.Add(endPt);
                
            }
            if (listPts != null)
                foreach (var p in listPts)
                    if (doc.Objects.AddPoint(p) != Guid.Empty)
            doc.Views.Redraw();

            // Connectivity Matrix
            var gp = listPts;
            var tp = listCrv;
            int nk = gp.Count;//number of nodes
            int nm = tp.Count;//number of members
            var C = ln.Matrix<double>.Build.Dense(nm, nk);
            int[] istart = new int[nm];
            int[] iend = new int[nm];

            for (int i = 0; i < nm; i++)
            {
                istart[i] = Rhino.Collections.Point3dList.ClosestIndexInList(gp, tp[i].PointAtStart);
                iend[i] = Rhino.Collections.Point3dList.ClosestIndexInList(gp, tp[i].PointAtEnd);
                C[i, istart[i]] = -1;
                C[i, iend[i]] = 1;
            }

            return Result.Success;
        }

    }
}
