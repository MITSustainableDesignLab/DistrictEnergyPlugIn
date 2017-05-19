using System;
using System.Drawing;
using System.Text;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using NetworkDraw.Geometry;
using Mit.Umi.RhinoServices;
using System.Collections.Generic;
using TrnsysUmiPlatform;
using System.Linq;

namespace NetworkDraw
{
    public class NetworkDrawCommand : Command
    {
        public override string EnglishName
        {
            get
            {
                return "CrossNetwork";
            }
        }

        public override Guid Id
        {
            get
            {
                return new Guid("{A98535DA-B2F5-477D-807D-28481965584B}");
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            SearchMode sm = SearchMode.CurveLength;

            Curve[] curves;
            OptionToggle tog = new OptionToggle(false, "Hide", "Show");
            OptionDouble tol = new OptionDouble(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, true, 0.0);
            OptionInteger sclfct = new OptionInteger(4, 1, 100);
            using (CurvesGetter getLines = new CurvesGetter("Select curves meeting at endpoints. Press Enter when done"))
            {
                for (;;)
                {
                    getLines.ClearCommandOptions();
                    getLines.EnableClearObjectsOnEntry(false);
                    int showInt = getLines.AddOptionToggle("Topology", ref tog);
                    //int begInt = getLines.AddOptionToggle("Starting Point", ref beg);
                    int tolInt = getLines.AddOptionDouble("Tolerance", ref tol);
                    int scalefactor = getLines.AddOptionInteger("ScaleFactor", ref sclfct);
                    int modeInt = GetterExtension.AddEnumOptionList(getLines, sm);

                    if (getLines.Curves(1, 0, out curves))
                        break;
                    else
                    {
                        if (getLines.Result() == GetResult.Option)
                        {
                            if (getLines.Option().Index == modeInt)
                            {
                                sm = GetterExtension.RetrieveEnumOptionValue<SearchMode>
                                    (getLines.Option().CurrentListOptionIndex);
                            }
                            continue;
                        }
                        else
                        {

                            // Get all of the objects on the layer. If layername is bogus, you will
                            // just get an empty list back
                            string layername = "Heating Network";
                            Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(layername).Where(x=>x.ObjectType == Rhino.DocObjects.ObjectType.Curve).ToArray();
                            if (rhobjs == null || rhobjs.Length < 1)
                                return Rhino.Commands.Result.Cancel;

                            curves = new Curve[rhobjs.Length];

                            for (int i = 0; i < rhobjs.Length; i++)
                            {
                                GeometryBase geom = rhobjs[i].Geometry;
                                Curve x = geom as Curve;
                                if (x != null && x.IsValid)
                                {
                                    curves[i] = x;
                                }
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
            }
            CurvesTopology crvTopology = new CurvesTopology(curves, tol.CurrentValue);



            Guid[] ids = null;
            if (tog.CurrentValue)
                ids = CurvesTopologyPreview.Mark(crvTopology, Color.LightBlue, Color.LightCoral, Color.GreenYellow);

            List<int> walkFromIndex;

            Result wasSuccessful = Result.Cancel;
            List<Type31> Pipes = new List<Type31>();
            List<Type11> Diverters = new List<Type11>();
            List<int> walked = new List<int>();
            if (ThermalPlantsOnTopology.GetThermalPlantsPointOnTopology(crvTopology, out walkFromIndex) != Result.Success)
            {
                RhinoApp.WriteLine("Error: No Thermal plant is defined on layer Thermal Plants");
                return Result.Failure;
            }

            List<int> walkToIndex;
            List<Guid> bldId;
            double[] distances;
            using (var getEnd = new PointGetter(crvTopology, walkFromIndex[0], sm)) //Can only do 1 thermal plant
            {
                if (getEnd.GetBuildingPointOnTopology(out walkToIndex, out bldId) != Result.Success)
                {
                    return Result.Failure;
                }
                distances = getEnd.DistanceCache;
            }

            if (walkToIndex.Contains(walkFromIndex[0]))
            {
                RhinoApp.WriteLine("Start and end points are equal");
                EndOperations(ids);
                return Result.Nothing;
            }

            PathMethod pathSearch = PathMethod.FromMode(sm, crvTopology, distances);

            int[] nIndices, eIndices;
            bool[] eDirs;
            double totLength;

            for (int i = 0; i < walkToIndex.Count; i++)
            {

                Curve c =
                    pathSearch.Cross(walkFromIndex[0], walkToIndex[i], out nIndices, out eIndices, out eDirs, out totLength);


                if (c != null && c.IsValid)
                {
                    if (tog.CurrentValue)
                    {
                        RhinoApp.WriteLine("Vertices: {0}", FormatNumbers(nIndices));
                        RhinoApp.WriteLine("Edges: {0}", FormatNumbers(eIndices));
                    }
                    for (int j = 0; j < eIndices.Length; j++)
                    {
                        int[,] inputs = { { 0, 0, 0 }, { 0, 0, 0 } };

                        Type31 pipe = new Type31(0.2, crvTopology.CurveAt(eIndices[j]).GetLength(), 1, 1000, 4.29, 20);

                        // Get the active view's construction plane
                        var view = doc.Views.ActiveView;
                        if (view == null)
                            return Rhino.Commands.Result.Failure;
                        var plane = view.ActiveViewport.ConstructionPlane();

                        var bbox = crvTopology.CurveAt(eIndices[j]).GetBoundingBox(plane);
                        if (!bbox.IsValid)
                            return Rhino.Commands.Result.Failure;


                        int[] fromOutputs = { 1, 2, 0 };
                        bool contains = false;

                        // If this is the first pipe, skip this part
                        if (j > 0)
                        {
                            int prev = j - 1;
                            int prevUnit = Pipes.Find(p => p.EdgeId == eIndices[prev]).Unit_number;
                            int[] fromUnit = { prevUnit, prevUnit, 0 };

                            // Test if need to create a diverter
                            int start;
                            if (eDirs[j])
                            {
                                start = crvTopology.EdgeAt(eIndices[j]).A;
                            }
                            else
                            {
                                start = crvTopology.EdgeAt(eIndices[j]).B;
                            }

                            // Only if the start nodeId of the pipe is an element of the List of Indices crossed by the curve
                            if (Array.Exists(nIndices, element => element.Equals(start)))
                            {
                                // If the nodeId is a diverter, then create a new diverter
                                if (crvTopology.NodeAt(start).IsDiverter)
                                {
                                    Type11 diverter = new Type11();
                                    diverter.SetInputs(fromUnit, fromOutputs);
                                    diverter.Unit_name = "Diverter_" + start.ToString();

                                    Plane worldPlane = Rhino.Geometry.Plane.WorldXY;
                                    Transform xform = Rhino.Geometry.Transform.ChangeBasis(worldPlane, plane);
                                    Point3d pos = new Point3d(crvTopology.VertexAt(start).X, crvTopology.VertexAt(start).Y, 0);
                                    pos.Transform(xform);
                                    diverter.Position = new double[2] { pos.X * sclfct.CurrentValue, pos.Y * sclfct.CurrentValue };
                                    diverter.NodeId = start;
                                    fromUnit.SetValue(diverter.Unit_number, 0); // resets the "from unit" number to the diverter's Unit_number
                                    fromUnit.SetValue(diverter.Unit_number, 1); // Idem

                                    // If that specific diverter ID has already been created, 
                                    // don't add it to the list but change the outputs, else add it to the list
                                    if (Diverters.Exists(d => d.NodeId == diverter.NodeId))
                                    {
                                        fromUnit.SetValue(Diverters.Find(d => d.NodeId == start).Unit_number, 0); // resets the from unit number to the diverter
                                        fromUnit.SetValue(Diverters.Find(d => d.NodeId == start).Unit_number, 1);
                                        fromOutputs.SetValue(Diverters.Find(d => d.NodeId == start).OutUsed ? 3:1, 0); // sets the input of the next pipe to the availabe output of the diverter (since it has two)
                                        fromOutputs.SetValue(Diverters.Find(d => d.NodeId == start).OutUsed ? 4:2, 1);
                                    }
                                    else
                                        Diverters.Add(diverter);
                                }
                            }

                            // Create the pipe
                            pipe.SetInputs(fromUnit, fromOutputs);
                            pipe.Unit_name = "Pipe_" + eIndices[j].ToString();
                            pipe.Position = new double[2] { bbox.Center.X * sclfct.CurrentValue, bbox.Center.Y * sclfct.CurrentValue };
                            pipe.EdgeId = eIndices[j];
                            contains = Pipes.Exists(p => p.EdgeId == pipe.EdgeId);
                        }
                        else
                        {
                            pipe.SetInputs(new int[3] { 0, 0, 0 }, fromOutputs);
                            pipe.Unit_name = "Pipe_" + eIndices[j].ToString();
                            pipe.Position = new double[2] { bbox.Center.X * sclfct.CurrentValue, bbox.Center.Y * sclfct.CurrentValue };
                            pipe.EdgeId = eIndices[j];
                            contains = Pipes.Exists(p => p.EdgeId == pipe.EdgeId);
                        }

                        if (contains != true)
                            Pipes.Add(pipe);
                    }
                }
                else
                {
                    RhinoApp.WriteLine("No path was found. Nodes are isolated.");
                    wasSuccessful = Result.Nothing;
                }
            }
            TrnsysModel model = new TrnsysModel("name", 1, GlobalContext.ActiveEpwPath, "Sam {i}", "description", @"C:\UMI\temp");
            WriteDckFile b = new WriteDckFile(model, Pipes, Diverters);

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

            StringBuilder s = new StringBuilder(arr[0].ToString());
            for (int i = 1; i < arr.Length; i++)
            {
                s.Append(", ");
                s.Append(arr[i].ToString());
            }
            return s.ToString();
        }


    }
}
