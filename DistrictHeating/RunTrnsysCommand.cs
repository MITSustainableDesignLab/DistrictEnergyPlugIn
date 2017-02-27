using System;
using Rhino;
using Rhino.Commands;
using TrnsysUmiPlatform;
using Rhino.DocObjects;
using Mit.Umi.Core;
using Mit.Umi.RhinoServices;
using Mit.Umi.RhinoServices.RhinoWrappers;
using Rhino.ApplicationSettings;

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
            try
            {
                // Gather Rhino doc objects to be translated into Trnsys components

                int layerIndex = doc.Layers.Find("Heating Network", true);
                Layer lay = doc.Layers[layerIndex];
                RhinoObject[] objs = doc.Objects.FindByLayer(lay);


                TrnsysModel trnsys_model =  new TrnsysModel(doc.Name,1, "CAN_PQ_Montreal.Intl.AP.716270_CWEC","Samuel Letellier-Duchesne","No Description", FileSettings.WorkingFolder);
                RhinoApp.WriteLine(FileSettings.WorkingFolder);

                var b = new WriteDckFile(trnsys_model, objs);

                var c = new RunTrnsys(trnsys_model);
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"Error: {e.Message}");
                return Result.Failure;
            }

            return Result.Success;
        }
    }
}
