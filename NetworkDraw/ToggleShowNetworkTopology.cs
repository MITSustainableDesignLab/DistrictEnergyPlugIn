using System;
using System.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using NetworkDraw.Geometry;

namespace NetworkDraw
{
    [System.Runtime.InteropServices.Guid("37e09539-facf-4c22-84f3-c1dd74fc1253")]
    public class ToggleShowNetworkTopology : Command
    {
        static ToggleShowNetworkTopology _instance;
        public ToggleShowNetworkTopology()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ShowNetworkTopology command.</summary>
        public static ToggleShowNetworkTopology Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "ToggleShowNetworkTopology"; }
        }

        public Guid[] ids { get; set; }
        bool toggle = false;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            toggle = !toggle;
            if (toggle)
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

                OptionDouble tol = new OptionDouble(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, true, 0.0);

                CurvesTopology crvTopology = new CurvesTopology(curves, tol.CurrentValue);

                ids = CurvesTopologyPreview.Mark(crvTopology, Color.LightBlue, Color.LightCoral, Color.GreenYellow);

                return Result.Success;

            }
            else
            {
                EndOperations(ids);
                return Result.Success;
            }
        }
            
        private static void EndOperations(Guid[] ids)
        {
            CurvesTopologyPreview.Unmark(ids);
            RhinoDoc.ActiveDoc.Views.Redraw();
        }
    }
}
