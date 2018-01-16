using Rhino;
using Rhino.ApplicationSettings;
using Rhino.Commands;
using Rhino.DocObjects;
using System;
using Mit.Umi.RhinoServices.Context;
using TrnsysUmiPlatform;

namespace DistrictEnergy
{
    [System.Runtime.InteropServices.Guid("4c307111-99c9-4287-9fd5-c2d86393ea7d")]
    public class DHRunTrnsysCommand : Command
    {
        static DHRunTrnsysCommand _instance;


        public DHRunTrnsysCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the RunTrnsysExe command.</summary>
        public static DHRunTrnsysCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DHRunTrnsysCommand"; }
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
                    string weather = UmiContext.Current.WeatherFilePath.ToString();
                    TrnsysModel trnsys_model = new TrnsysModel(doc.Name, 1, weather, "Samuel Letellier-Duchesne", "No Description", FileSettings.WorkingFolder);
                    RhinoApp.WriteLine("The working Trnsys folder is : " + FileSettings.WorkingFolder);

                    // Get building loads

                    // Run
                    //var b = new TrnsysDckFile(trnsys_model, rhobjs);

                    trnsys_model.RunTrnsys(false);
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
