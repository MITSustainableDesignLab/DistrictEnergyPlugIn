using System;
using Rhino;
using Rhino.Commands;
using TrnsysUmiPlatform;
using Rhino.DocObjects;
using Mit.Umi.Core;
using Mit.Umi.RhinoServices;
using Mit.Umi.RhinoServices.RhinoWrappers;
using Rhino.ApplicationSettings;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
                Rhino.DocObjects.RhinoObject[] objs = doc.Objects.FindByLayer(lay);
                string weather = GlobalContext.ActiveEpwPath.ToString();
                TrnsysModel trnsys_model =  new TrnsysModel(doc.Name,1, weather, "Samuel Letellier-Duchesne","No Description", FileSettings.WorkingFolder);
                RhinoApp.WriteLine("The working Trnsys folder is : " + FileSettings.WorkingFolder);

                // Get building loads
                
                // Run
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
