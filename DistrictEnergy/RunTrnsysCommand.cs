using Mit.Umi.RhinoServices;
using Rhino;
using Rhino.ApplicationSettings;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using TrnsysUmiPlatform;

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
                Rhino.DocObjects.RhinoObject[] rhobjs = doc.Objects.FindByLayer(lay);
                if (rhobjs.Length > 0)
                {
                    string weather = GlobalContext.ActiveEpwPath.ToString();
                    TrnsysModel trnsys_model = new TrnsysModel(doc.Name, 1, weather, "Samuel Letellier-Duchesne", "No Description", FileSettings.WorkingFolder);
                    RhinoApp.WriteLine("The working Trnsys folder is : " + FileSettings.WorkingFolder);

                    // Get building loads

                    // Run
                    //var b = new WriteDckFile(trnsys_model, rhobjs);

                    var c = new RunTrnsys(trnsys_model);
                }
                else
                {
                    throw new InvalidOperationException("No pipes found on layer Heating Network");
                }
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
