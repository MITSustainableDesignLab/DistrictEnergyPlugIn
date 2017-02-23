using System;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;

namespace DistrictEnergy
{
    [System.Runtime.InteropServices.Guid("7f1c3523-3d22-45df-9466-9fdf5930c41c")]
    public class OrderNetwork : Command
    {
        static OrderNetwork _instance;
        public OrderNetwork()
        {
            _instance = this;
        }

        ///<summary>The only instance of the OrderNetwork command.</summary>
        public static OrderNetwork Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "OrderNetwork"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Select starting pipe
            ObjRef startPipe;
            Result rc = Rhino.Input.RhinoGet.GetOneObject("Select Starting Pipe", false, ObjectType.Curve, out startPipe);
            Rhino.Geometry.Curve crv = startPipe.Curve();
            if (crv == null)
                return Rhino.Commands.Result.Failure;

            var a = crv.PointAtStart;
            // Check for direction reversion


            //

            return Result.Success;
        }
    }
}
