using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using NetworkDraw.Geometry;
using System.Collections.Generic;
using Mit.Umi.RhinoServices;
using System.Linq;
using NetworkDraw;
using System.Collections;

namespace DistrictEnergy.Metrics
{
    [System.Runtime.InteropServices.Guid("fb5ded40-ae61-4174-9130-cb0e90e1bcec")]
    public class DHNetworkListCommand : Command
    {
        static DHNetworkListCommand _instance;
        private static Dictionary<int,List<int>> dic = new Dictionary<int, List<int>>();
        private static Dictionary<int,double> BuildingLoads = new Dictionary<int, double>();

        /// <summary>Gets the only instance of the NetworkList command.</summary>
        public DHNetworkListCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the NetworkList command.</summary>
        public static DHNetworkListCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DHNetworkList"; }
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
            List<bool[]> myBools = new List<bool[]>();

            // Create a list of routes
            for (int i = 0; i < walkToIndex.Count; i++)
            {
                Curve c =
                    pathSearch.Cross(walkFromIndex[0], walkToIndex[i], out nIndices, out eIndices, out eDirs, out totLength);
                Array.Reverse(nIndices);
                myList.Add(nIndices);
                myBools.Add(eDirs);
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

            //Dictionary<int, List<int>> dic = new Dictionary<int, List<int>>();
            for (int i = 0; i < NodeLoad.Count; i++)
            {
                if (NodeLoad[i].Count > 0)
                    dic.Add(i, NodeLoad[i].ToList());
                else
                    continue;
            }

            var MaxHeatinLoadQuery = GlobalContext.GetObjects().
                Select(b => new
                { BuildingId = b.Id, MaxLoad = MaxHeatingLoad(b), BldNode = bldIndex.FirstOrDefault(x => x.Value.ToString() == b.Id).Key });

            for (int i=0;i< MaxInt; i++)
            {
                var MaxLoad = MaxHeatinLoadQuery.Where(x => x.BldNode == i).Select(y => y.MaxLoad).Sum();
                if (MaxLoad > 0)
                    BuildingLoads.Add(i, MaxLoad);
                else
                    continue;
            }

            //Calculate optimal diameter
            List<double> PipeDiam = new List<double>();
            for (int i=0;i < crvTopology.EdgeLength;i++)
            {
                double maxLoadatB = MaxLoadAt(crvTopology.EdgeAt(i).B);
                PipeDiam.Add(maxLoadatB);
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

        class TreeNode : IEnumerable<TreeNode>
        {
            private readonly Dictionary<int, TreeNode> _children =
                                                new Dictionary<int, TreeNode>();

            public readonly int ID;
            public TreeNode Parent { get; private set; }

            public TreeNode(int id)
            {
                this.ID = id;
            }

            public TreeNode GetChild(int id)
            {
                return this._children[id];
            }

            public void Add(TreeNode item)
            {
                if (item.Parent != null)
                {
                    item.Parent._children.Remove(item.ID);
                }

                item.Parent = this;
                this._children.Add(item.ID, item);
            }

            public IEnumerator<TreeNode> GetEnumerator()
            {
                return this._children.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public int Count
            {
                get { return this._children.Count; }
            }
        }

        private List<int> GetAllChildren(int parent, Dictionary<int,List<int>> dic)
        {
            List<int> children = new List<int>();
            PopulateChildren(parent, children, dic);
            return children;
        }
        private void PopulateChildren(int parent, List<int> children, Dictionary<int, List<int>> dic)
        {
            List<int> myChildren;
            if (dic.TryGetValue(parent, out myChildren))
            {
                children.AddRange(myChildren);
                foreach (int child in myChildren)
                {
                    PopulateChildren(child, children, dic);
                }
            }
        }
        private static void AddEntry(int parent, int child, Dictionary<int, List<int>> dic)
        {
            List<int> children;
            if (!dic.TryGetValue(parent, out children))
            {
                children = new List<int>();
                dic[parent] = children;
            }
            children.Add(child);
        }

        /// <summary>
        /// Returns the maximum load at a node.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public double MaxLoadAt(int nodeId) => dic.ContainsKey(nodeId) ? dic[nodeId].Sum(n => MaxLoadAt(n)) : BuildingLoads.Where(y => y.Key == nodeId).Select(x => x.Value).Sum();

    }
}