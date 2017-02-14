using System;
using Rhino;
using Rhino.Commands;

namespace DistrictEnergy
{
    [System.Runtime.InteropServices.Guid("4c307111-99c9-4287-9fd5-c2d86393ea7d")]
    public class RunTrnsys : Command
    {
        static RunTrnsys _instance;
        public RunTrnsys()
        {
            _instance = this;
        }

        ///<summary>The only instance of the RunTrnsys command.</summary>
        public static RunTrnsys Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "RunTrnsys"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            return Result.Success;
        }
    }
}
