using System;
using Rhino;
using Rhino.Commands;
using DistrictEnergy.TRNSYS;

namespace DistrictEnergy
{
    [System.Runtime.InteropServices.Guid("4c307111-99c9-4287-9fd5-c2d86393ea7d")]
    public class RunTrnsysCommand : Command
    {
        static RunTrnsysCommand _instance;
        public RunTrnsysCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the RunTrnsys command.</summary>
        public static RunTrnsysCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "RunTrnsysCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.

            var trnsys_model = new TrnsysModel();
            trnsys_model.PlantSelection = "Plant 1";
            trnsys_model.ModelName = "MyModel";
            trnsys_model.WeatherFile = "CAN_PQ_Montreal.Intl.AP.716270_CWEC";
            trnsys_model.HourlyTimestep = 0.25;

            var b = new WriteDckFile(trnsys_model);

            var c = new RunTrnsys(trnsys_model);

            return Result.Success;
        }
    }
}
