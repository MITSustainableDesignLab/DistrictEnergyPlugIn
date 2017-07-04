using System;
using System.Collections.Generic;
using System.Drawing;
using NetworkDraw.Geometry;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Linq;
using TrnsysUmiPlatform.Types;

namespace NetworkDraw
{
    [System.Runtime.InteropServices.Guid("9bd7c1b2-79ed-46a9-a281-f1744d3eaab7")]
    public class DhCrosNetworkCommandV2 : Command
    {
        static DhCrosNetworkCommandV2 _instance;
        public DhCrosNetworkCommandV2()
        {
            _instance = this;
        }

        ///<summary>The only instance of the DhCrosNetworkCommandV2 command.</summary>
        public static DhCrosNetworkCommandV2 Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DhCrosNetworkCommandV2"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            var sm = SearchMode.CurveLength;

            Curve[] curves;
            var tog = new OptionToggle(false, "Hide", "Show");
            var tol = new OptionDouble(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, true, 0.0);
            var sclfct = new OptionInteger(4, 1, 100);
            using (var getLines = new CurvesGetter("Select curves meeting at endpoints. Press Enter when done"))
            {
                for (;;)
                {
                    getLines.ClearCommandOptions();
                    getLines.EnableClearObjectsOnEntry(false);
                    var showInt = getLines.AddOptionToggle("Topology", ref tog);
                    //int begInt = getLines.AddOptionToggle("Starting Point", ref beg);
                    var tolInt = getLines.AddOptionDouble("Tolerance", ref tol);
                    var scalefactor = getLines.AddOptionInteger("ScaleFactor", ref sclfct);
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
            var crvTopology = new CurvesTopology(curves,tol.CurrentValue);

            Guid[] ids = null;
            if (tog.CurrentValue)
                ids = CurvesTopologyPreview.Mark(crvTopology, Color.LightBlue, Color.LightCoral, Color.GreenYellow);

            List<int> walkFromIndex;

            var wasSuccessful = Result.Cancel;
            var pipes = new List<Type31>();
            var diverters = new List<int>();
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
                if (getEnd.GetBuildingPointOnTopology(out walkToIndex,out bldId) != Result.Success)
                    return Result.Failure;
                distances = getEnd.DistanceCache;
            }

            if (walkToIndex.Contains(walkFromIndex[0]))
            {
                RhinoApp.WriteLine("Start and end points are equal");
                EndOperations(ids);
                return Result.Nothing;
            }

            PathMethod pathSearch = PathMethod.FromMode(sm,crvTopology,distances);

            int[] eIndices;

            



                return Result.Success;
        }

        private static void EndOperations(Guid[] ids)
        {
            CurvesTopologyPreview.Unmark(ids);
            RhinoDoc.ActiveDoc.Views.Redraw();
        }
    }
}
