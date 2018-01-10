using Mit.Umi.RhinoServices;
using NetworkDraw.Geometry;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrnsysUmiPlatform;
using TrnsysUmiPlatform.Types;

namespace NetworkDraw
{
    [System.Runtime.InteropServices.Guid("9bd7c1b2-79ed-46a9-a281-f1744d3eaab7")]
    public class DHCrossNetworkCommand : Command
    {
        static DHCrossNetworkCommand _instance;
        public DHCrossNetworkCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the DHCrossNetworkCommand command.</summary>
        public static DHCrossNetworkCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DHCrossNetworkCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            var sm = SearchMode.CurveLength;

            Curve[] curves;
            var tog = new OptionToggle(false, "Hide", "Show");
            var tol = new OptionDouble(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, true, 0.0);
            var scaleFactor = new OptionInteger(4, 1, 100);
            using (var getLines = new CurvesGetter("Select curves meeting at endpoints. Press Enter when done"))
            {
                for (;;)
                {
                    getLines.ClearCommandOptions();
                    getLines.EnableClearObjectsOnEntry(false);
                    var showInt = getLines.AddOptionToggle("Topology", ref tog);
                    //int begInt = getLines.AddOptionToggle("Starting Point", ref beg);
                    var tolInt = getLines.AddOptionDouble("Tolerance", ref tol);
                    var scalefactor = getLines.AddOptionInteger("ScaleFactor", ref scaleFactor);
                    var modeInt = GetterExtension.AddEnumOptionList(getLines, sm);

                    if (getLines.Curves(1, 0, out curves))
                        break;
                    if (getLines.Result() == GetResult.Option)
                    {
                        if (getLines.Option().Index == modeInt)
                            sm = GetterExtension.RetrieveEnumOptionValue<SearchMode>
                                (getLines.Option().CurrentListOptionIndex);
                    }
                    else
                    {
                        // Get all of the objects on the layer. If layername is bogus, you will
                        // just get an empty list back
                        const string layername = "Heating Network";
                        var rhobjs = doc.Objects.FindByLayer(layername).Where(x => x.ObjectType == ObjectType.Curve)
                            .ToArray();
                        if (rhobjs == null || rhobjs.Length < 1)
                            return Result.Cancel;

                        curves = new Curve[rhobjs.Length];

                        for (var i = 0; i < rhobjs.Length; i++)
                        {
                            GeometryBase geom = rhobjs[i].Geometry;
                            var x = geom as Curve;
                            if (x != null && x.IsValid)
                                curves[i] = x;
                        }
                        if (curves.Length == 0)
                        {
                            RhinoApp.WriteLine("Less than three lines are on the layer");
                            return Result.Cancel;
                        }
                        break;
                    }
                }
            }
            var crvTopology = new CurvesTopology(curves, tol.CurrentValue);

            Guid[] ids = null;
            if (tog.CurrentValue)
                ids = CurvesTopologyPreview.Mark(crvTopology, Color.LightBlue, Color.LightCoral, Color.GreenYellow);

            List<int> walkFromIndex;

            var wasSuccessful = Result.Cancel;
            var pipes = new List<Type31>();
            var diverters = new List<Type11>();
            var walked = new List<int>();

            if (ThermalPlantsOnTopology.GetThermalPlantsPointOnTopology(crvTopology, out walkFromIndex) !=
                Result.Success)
            {
                RhinoApp.WriteLine("Error: No Thermal plant is defined on layer Thermal Plants");
                return Result.Failure;
            }

            List<int> walkToIndex;
            List<Guid> bldId;
            double[] distances;

            using (var getEnd = new PointGetter(crvTopology, walkFromIndex[0], sm)) //Can only di 1 thermal plant
            {
                if (getEnd.GetBuildingPointOnTopology(out walkToIndex, out bldId) != Result.Success)
                    return Result.Failure;
                distances = getEnd.DistanceCache;
            }

            if (walkToIndex.Contains(walkFromIndex[0]))
            {
                RhinoApp.WriteLine("Start and end points are equal");
                EndOperations(ids);
                return Result.Nothing;
            }

            PathMethod pathSearch = PathMethod.FromMode(sm, crvTopology, distances);

            int[] eIndices;

            for (var i = walkToIndex.Count - 1; i >= 0; i--)
            {
                double totLength;
                bool[] eDirs;
                int[] nIndices;
                Curve curve =
                    pathSearch.Cross(walkFromIndex[0], i, out nIndices, out eIndices, out eDirs, out totLength);


                if (curve != null && curve.IsValid)
                {
                    if (tog.CurrentValue)
                    {
                        RhinoApp.WriteLine("Vertices: {0}", FormatNumbers(nIndices));
                        RhinoApp.WriteLine("Edges: {0}", FormatNumbers(eIndices));
                    }
                    for (var j = 0; j < eIndices.Length; j++)
                    {
                        var pipe = new Type31(0.2, crvTopology.CurveAt(eIndices[j]).GetLength(), 1, 1000, 4.29, 20);

                        Plane plane = GetActivePlane(doc);

                        BoundingBox bbox = crvTopology.CurveAt(eIndices[j]).GetBoundingBox(plane);
                        if (!bbox.IsValid)
                            return Result.Failure;


                        int[] fromOutputs = { 1, 2, 0 };
                        var contains = false;

                        // If this is the first pipe, skip the diverter creation
                        if (j > 0)
                        {
                            var prev = j - 1;
                            var prevUnit = pipes.Find(p => p.EdgeId == eIndices[prev]).UnitNumber;
                            int[] fromUnit = { prevUnit, prevUnit, 0 };

                            // Test if need to create a diverter
                            int start;
                            start = eDirs[j] ? crvTopology.EdgeAt(eIndices[j]).A : crvTopology.EdgeAt(eIndices[j]).B;

                            // Only if the start nodeId of the pipe is an element of the List of Indices crossed by the curve
                            if (Array.Exists(nIndices, element => element.Equals(start)) &&
                                crvTopology.NodeAt(start).IsDiverter)
                            {
                                var diverter = new Type11();
                                diverter.SetInputs(fromUnit, fromOutputs);
                                diverter.UnitName = "Diverter_" + start;

                                Point3d position = GetPosition(crvTopology, start, plane);
                                diverter.Position = new double[]
                                    {position.X * scaleFactor.CurrentValue, position.Y * scaleFactor.CurrentValue};

                                diverter.NodeId = start;
                                fromUnit.SetValue(diverter.UnitNumber,
                                    0); // resets the "from unit" number to the diverter's Unit_number
                                fromUnit.SetValue(diverter.UnitNumber, 1); // Idem

                                // If that specific diverter ID has already been created, 
                                // don't add it to the list but change the outputs, else add it to the list
                                if (diverters.Exists(d => d.NodeId == diverter.NodeId))
                                {
                                    fromUnit.SetValue(diverters.Find(d => d.NodeId == start).UnitNumber,
                                        0); // resets the from unit number to the diverter
                                    fromUnit.SetValue(diverters.Find(d => d.NodeId == start).UnitNumber, 1);
                                    fromOutputs.SetValue(diverters.Find(d => d.NodeId == start).OutUsed ? 3 : 1,
                                        0); // sets the input of the next pipe to the availabe output of the diverter (since it has two)
                                    fromOutputs.SetValue(diverters.Find(d => d.NodeId == start).OutUsed ? 4 : 2, 1);
                                }
                                else
                                {
                                    diverters.Add(diverter);
                                }
                            }

                            // Create the pipe

                            pipe.SetInputs(fromUnit, fromOutputs);
                            pipe.UnitName = "Pipe_" + eIndices[j];
                            pipe.Position = new double[]
                                {bbox.Center.X * scaleFactor.CurrentValue, bbox.Center.Y * scaleFactor.CurrentValue};
                            pipe.EdgeId = eIndices[j];
                            contains = pipes.Exists(p => p.EdgeId == pipe.EdgeId);
                        }
                        else
                        {
                            pipe.SetInputs(new int[] { 0, 0, 0 }, fromOutputs);
                            pipe.UnitName = "Pipe_" + eIndices[j];
                            pipe.Position = new double[]
                                {bbox.Center.X * scaleFactor.CurrentValue, bbox.Center.Y * scaleFactor.CurrentValue};
                            pipe.EdgeId = eIndices[j];
                            contains = pipes.Exists(p => p.EdgeId == pipe.EdgeId);
                        }

                        if (contains != true)
                            pipes.Add(pipe);
                    }
                }
                else
                {
                    RhinoApp.WriteLine("No path was found. Nodes are isolated.");
                    wasSuccessful = Result.Nothing;
                }
            }


            var model = new TrnsysModel("name", 1, GlobalContext.ActiveEpwPath, "Sam {i}", "description",
                @"C:\UMI\temp");
            model.WriteDckFile(pipes, diverters);
            Task.Factory.StartNew(() => { model.RunTrnsys(false); });

            EndOperations(ids);
            return wasSuccessful;
        }

        private static void EndOperations(Guid[] ids)
        {
            CurvesTopologyPreview.Unmark(ids);
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private static string FormatNumbers(int[] arr)
        {
            if (arr == null || arr.Length == 0)
                return "(empty)";

            var s = new StringBuilder(arr[0].ToString());
            for (var i = 1; i < arr.Length; i++)
            {
                s.Append(", ");
                s.Append(arr[i].ToString());
            }
            return s.ToString();
        }

        private static Plane GetActivePlane(RhinoDoc doc)
        {
            // Get the active view's construction plane
            RhinoView activeView = doc.Views.ActiveView;

            Plane plane = activeView.ActiveViewport.ConstructionPlane();

            return plane;
        }

        private static Point3d GetPosition(CurvesTopology crvTopology, int start, Plane plane)
        {
            Plane localPlane = plane;
            Plane worldPlane = Plane.WorldXY;
            Transform xform = Transform.ChangeBasis(worldPlane, localPlane);
            var pos = new Point3d(crvTopology.VertexAt(start).X, crvTopology.VertexAt(start).Y,
                0);
            pos.Transform(xform);
            return pos;
        }
    }
}
