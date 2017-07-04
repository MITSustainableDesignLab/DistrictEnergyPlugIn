﻿using NetworkDraw.Geometry;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using TrnsysUmiPlatform.Types;

namespace NetworkDraw
{
    public class ShortestPathInCurvesCommand : Command
    {
        public override string EnglishName
        {
            get
            {
                return "DHNetworkDraw";
            }
        }

        public override Guid Id
        {
            get
            {
                return new Guid("{94838428-8c17-4db6-a652-595dfd71ff03}");
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            SearchMode sm = SearchMode.CurveLength;

            Curve[] curves;
            OptionToggle tog = new OptionToggle(false, "Hide", "Show");
            //OptionToggle beg = new OptionToggle(false, "FromLast", "FromStart");
            OptionDouble tol = new OptionDouble(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, true, 0.0);
            using (CurvesGetter getLines = new CurvesGetter("Select curves meeting at endpoints. Press Enter when done"))
            {
                for (;;)
                {
                    getLines.ClearCommandOptions();
                    getLines.EnableClearObjectsOnEntry(false);
                    int showInt = getLines.AddOptionToggle("Topology", ref tog);
                    //int begInt = getLines.AddOptionToggle("Starting Point", ref beg);
                    int tolInt = getLines.AddOptionDouble("Tolerance", ref tol);
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
                            RhinoApp.WriteLine("Less than three lines were selected");
                            return Result.Cancel;
                        }
                    }
                }
            }
            CurvesTopology crvTopology = new CurvesTopology(curves, tol.CurrentValue);



            Guid[] ids = null;
            if (tog.CurrentValue)
                ids = CurvesTopologyPreview.Mark(crvTopology, Color.LightBlue, Color.LightCoral, Color.GreenYellow);

            int walkFromIndex;
            using (var getStart = new TrackingPointGetter("Select the start point of the walk on the curves", crvTopology))
            {
                if (getStart.GetPointOnTopology(out walkFromIndex) != Result.Success)
                {
                    EndOperations(ids);
                    return Result.Cancel;
                }
            }

            Result wasSuccessful = Result.Cancel;
            List<Type31> Pipes = new List<Type31>();
            for (;;)
            {

                int walkToIndex;
                double[] distances;
                using (var getEnd = new TrackingPointGetter("Select the end point", crvTopology, walkFromIndex, sm))
                {
                    if (getEnd.GetPointOnTopology(out walkToIndex) != Result.Success)
                    {
                        break;
                    }
                    distances = getEnd.DistanceCache;
                }

                if (walkFromIndex == walkToIndex)
                {
                    RhinoApp.WriteLine("Start and end points are equal");
                    EndOperations(ids);
                    return Result.Nothing;
                }

                PathMethod pathSearch = PathMethod.FromMode(sm, crvTopology, distances);

                int[] nIndices, eIndices;
                bool[] eDirs;
                double totLength;
                Curve c =
                    pathSearch.Cross(walkFromIndex, walkToIndex, out nIndices, out eIndices, out eDirs, out totLength);

                if (c != null && c.IsValid)
                {
                    if (tog.CurrentValue)
                    {
                        RhinoApp.WriteLine("Vertices: {0}", FormatNumbers(nIndices));
                        RhinoApp.WriteLine("Edges: {0}", FormatNumbers(eIndices));
                    }

                    var a = RhinoDoc.ActiveDoc.CreateDefaultAttributes();
                    Guid g = RhinoDoc.ActiveDoc.Objects.AddCurve(c, a);

                    var obj = RhinoDoc.ActiveDoc.Objects.Find(g);
                    if (obj != null)
                    {
                        obj.Select(true);
                        wasSuccessful = Result.Success;
                        //if (beg.CurrentValue) //Here we would allow a user to choose between WalkFromIndex or WalkToIndex
                        //walkFromIndex = walkToIndex;


                    }
                    else
                    {
                        RhinoApp.WriteLine("An error occurred while adding the new polycurve.");
                        wasSuccessful = Result.Failure;
                        break;
                    }
                }
                else
                {
                    RhinoApp.WriteLine("No path was found. Nodes are isolated.");
                    wasSuccessful = Result.Nothing;
                }
            }

            RhinoApp.RunScript("Explode", true);
            RhinoApp.RunScript("Seldup", true);
            RhinoApp.RunScript("Delete", false);

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
