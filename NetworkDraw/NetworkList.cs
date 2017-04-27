using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using NetworkDraw.Geometry;
using System.Collections.Generic;
using Mit.Umi.RhinoServices;
using System.Linq;
using System.Collections.ObjectModel;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.Search;
using QuickGraph.Algorithms.Observers;
using System.Windows;

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
            #region Get Layer Content
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
            CurvesTopology crvTopology = new CurvesTopology(curves, 0.01);
            List<int> walkFromIndex;
            if (ThermalPlantsOnTopology.GetThermalPlantsPointOnTopology(crvTopology, out walkFromIndex) != Result.Success)
            {
                RhinoApp.WriteLine("Error: No Thermal plant is defined on layer Thermal Plants");
                return Result.Failure;
            }

            List<int> walkToIndex;
            List<Guid> bldId;
            double[] distances;
            using (var getEnd = new PointGetter(crvTopology, walkFromIndex[0], SearchMode.CurveLength)) //Can only do 1 thermal plant
            {
                if (getEnd.GetBuildingPointOnTopology(out walkToIndex, out bldId) != Result.Success)
                {
                    return Result.Failure;
                }
                distances = getEnd.DistanceCache;
            }

            var bldIndex = walkToIndex.Zip(bldId, (k, v) => new { Index = k, Guid = v })
                     .ToDictionary(x => x.Index, x => x.Guid);
            #endregion

            PathMethod pathSearch = PathMethod.FromMode(SearchMode.CurveLength, crvTopology, distances);

            int[] nIndices, eIndices;
            bool[] eDirs;
            double totLength;

            List<int[]> myList = new List<int[]>();

            // Create a list of routes
            for (int i = 0; i < walkToIndex.Count; i++)
            {
                Curve c =
                    pathSearch.Cross(walkFromIndex[0], walkToIndex[i], out nIndices, out eIndices, out eDirs, out totLength);
                Array.Reverse(nIndices);
                myList.Add(nIndices);
            }
            List<int[]> SorterdMyList = myList.OrderByDescending(x => x.Length).ToList();

            var MaxInt = crvTopology.VertexLength;
            List<List<int>> NodeLoad = new List<List<int>>(MaxInt);
            for (int i = 0; i < MaxInt; i++) NodeLoad.Add(new List<int>());

            foreach (int[] path in myList)
            {
                for (int indices = MaxInt; indices >= 0; indices--)
                {
                    int a = Array.FindIndex(path, o => o == indices);
                    if (a - 1 >= 0)
                    {
                        var b = a - 1;
                        var prevIndex = path[b];
                        if (NodeLoad[indices].Contains(prevIndex))
                            continue;
                        else
                        {
                            NodeLoad[indices].Add(prevIndex);
                        }
                    }
                }
            }

            Dictionary<int, int[]> dic = new Dictionary<int, int[]>();
            for (int i = 0; i < NodeLoad.Count; i++)
            {
                dic.Add(i, NodeLoad[i].ToArray());
            }

            var MaxHeatinLoadQuery = GlobalContext.GetObjects().
                Select(b => new
                { BuildingId = b.Id, MaxLoad = MaxHeatingLoad(b), BldNode = bldIndex.FirstOrDefault(x => x.Value.ToString() == b.Id).Key });

            int stop = 0;
            while (stop != 0)
            {
                foreach(var a in dic)
                {
                    double MaxLoad = MaxHeatinLoadQuery.Where(x => x.BldNode == a.Key).Select(y => y.MaxLoad).Sum();
                }
            }

            return Result.Success;

        }
        /// <summary>
        /// This finds the maximum heating Load (SP + DHW)
        /// </summary>
        /// <param name="building">A building IUmiObject</param>
        /// <returns></returns>
        public static double MaxHeatingLoad(Mit.Umi.Core.IUmiObject building)
        {
            return building.Data["SDL/Heating"].Data.Zip(building.Data["SDL/Domestic Hot Water"].Data, (heat, dhw) => heat + dhw).Max();
        }

        public class BldgIndex
        {
            public int BldIndex { get; set; }
            public Guid BldId { get; set; }
        }

        Dictionary<int, double> list = new Dictionary<int, double>();

        private double CallNode(TreeNode a, Dictionary<int,Guid> bldIndex)
        {
            if (a.ParentNodes.Count() == 0)
            {
                double HeatLoad = GlobalContext.GetObjects().
                Select(b => new
                { BuildingId = b.Id, MaxLoad = MaxHeatingLoad(b), BldNode = bldIndex.FirstOrDefault(x => x.Value.ToString() == b.Id).Key })
                .Where(y => y.BldNode == a.NodeId)
                .Select(x => x.MaxLoad).Sum();
                list.Add(a.NodeId, HeatLoad);
                return HeatLoad;
            }
            else
            {
                double sum = 0;
                foreach (TreeNode entry in a.ParentNodes)
                {
                    sum =+ CallNode(entry, bldIndex);
                }
                list.Add(a.NodeId, sum);
                return sum;
            }
            
        }

        class TreeNode
        {
            public int NodeId { get; set; }
            public TreeNode[] ParentNodes { get; set; }
        }
    }
}