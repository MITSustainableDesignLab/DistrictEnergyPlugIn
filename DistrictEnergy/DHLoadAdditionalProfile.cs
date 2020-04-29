using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using CsvHelper;
using DistrictEnergy.Networks.Loads;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Umi.Core;
using Umi.RhinoServices.Context;

namespace DistrictEnergy
{
    public class DHLoadAdditionalProfile : Command
    {
        public DHLoadAdditionalProfile()
        {
            Instance = this;
        }

        ///<summary>The only instance of the DHLoadAdditionalProfile command.</summary>
        public static DHLoadAdditionalProfile Instance { get; private set; }

        public override string EnglishName => "DHLoadAdditionalProfile";

        public static List<string> Types
        {
            get
            {
                var types = new List<string>();
                types.Add("Cooling");
                types.Add("Heating");
                types.Add("Elec");
                return types;
            }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var context = UmiContext.Current;
            var fileContent = string.Empty;
            var filePath = string.Empty;

            ObjRef obj_ref;
            var rc = RhinoGet.GetOneObject("Select a brep", true, ObjectType.Brep, out obj_ref);
            if (rc != Result.Success)
                return rc;

            var refId = obj_ref.ObjectId;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = doc.Path;
                openFileDialog.Filter = "Comma Separated Value | *.csv";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
            }

            //Read the contents of the file into the umi db
            AdditionalLoads.AddAdditionalLoad(filePath, context, refId);

            RhinoApp.WriteLine($"Added additional load from '{filePath}'");
            return Result.Success;
        }
    }
}