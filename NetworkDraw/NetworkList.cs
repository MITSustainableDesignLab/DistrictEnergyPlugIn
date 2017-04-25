using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using NetworkDraw.Geometry;
using System.Collections.Generic;
using Mit.Umi.RhinoServices;
using System.Linq;

namespace NetworkDraw
{
    [System.Runtime.InteropServices.Guid("fb5ded40-ae61-4174-9130-cb0e90e1bcec")]
    public class NetworkList : Command
    {
        static NetworkList _instance;
        public NetworkList()
        {
            _instance = this;
        }

        ///<summary>The only instance of the NetworkList command.</summary>
        public static NetworkList Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "NetworkList"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string layername = "Heating Network";

            // Get all of the objects on the layer. If layername is bogus, you will
            // just get an empty list back
            Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(layername);
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
            CurvesTopology crvTopology = new CurvesTopology(curves, 0.01);
            List<int> walkFromIndex;
            if (ThermalPlantsOnTopology.GetThermalPlantsPointOnTopology(crvTopology, out walkFromIndex) != Result.Success)
            {
                RhinoApp.WriteLine("Error: No Thermal plant is defined on layer Thermal Plants");
                return Result.Failure;
            }

            List<int> walkToIndex;
            double[] distances;
            using (var getEnd = new PointGetter(crvTopology, walkFromIndex[0], SearchMode.CurveLength)) //Can only do 1 thermal plant
            {
                if (getEnd.GetBuildingPointOnTopology(out walkToIndex) != Result.Success)
                {
                    return Result.Failure;
                }
                distances = getEnd.DistanceCache;
            }

            PathMethod pathSearch = PathMethod.FromMode(SearchMode.CurveLength, crvTopology, distances);

            int[] nIndices, eIndices;
            bool[] eDirs;
            double totLength;

            List<int[]> myList = new List<int[]>();

            for (int i = 0; i < walkToIndex.Count; i++)
            {
                Curve c =
                    pathSearch.Cross(walkFromIndex[0], walkToIndex[i], out nIndices, out eIndices, out eDirs, out totLength);
                Array.Reverse(nIndices);
                myList.Add(nIndices);
            }
            var MaxInt = walkToIndex.Max();
            List<List<int>> NodeLoad = new List<List<int>>();
            foreach (int[] path in myList)
            {
                for (int indices=0; indices < walkToIndex.Max();indices++)
                {
                    int a = Array.FindIndex(path, o => o == indices);
                    if (a-1 >= 0)
                    {
                        var b = a - 1;
                        var prevIndex = path[b];
                        //NodeLoad.Insert(a,prevIndex);
                    }
                }
            }

            for (int indices = 0; indices < walkToIndex.Max(); indices++)
            {
                RhinoApp.WriteLine("Node {0}'s load is from {1}", indices, NodeLoad[indices].ToString());
            }

            

            return Result.Success;


        }
    }
}