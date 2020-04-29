using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.ApplicationSettings;
using Rhino.Commands;
using Umi.RhinoServices.Context;

namespace DistrictEnergy
{
    /// <summary>
    /// 
    /// </summary>
    [System.Runtime.InteropServices.Guid("fcdf33a5-a428-490d-bf49-310dd3bab8d2")]
    public class CreateLoadProfilesFiles : Command
    {
        static CreateLoadProfilesFiles _instance;
        public CreateLoadProfilesFiles()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CreateLoadProfilesFiles command.</summary>
        public static CreateLoadProfilesFiles Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "CreateLoadProfilesFiles"; }
        }
        private const string delimiter = ",";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            {
                // Creating the folder into with the load profiles files will be saved
                string workspacepath = FileSettings.WorkingFolder.ToString();
                string extractionfolder = FileSettings.WorkingFolder.ToString() + "\\Trnsys";
                string BundlePath = FileSettings.WorkingFolder.ToString();

                bool exists1 = System.IO.Directory.Exists(workspacepath);
                if (!exists1)
                    System.IO.Directory.CreateDirectory(workspacepath);

                bool exists2 = System.IO.Directory.Exists(extractionfolder);
                if (!exists2)
                    System.IO.Directory.CreateDirectory(extractionfolder);


                // For each objects in the global context, create one csv file containing two columns
                // The first column is the simulation timestep and the second column is the the values 
                // of the selected output

                foreach (var o in UmiContext.Current.GetObjects())
                {
                    // Create and Save File
                    string csvFileName = o.Id.ToString();
                    string filePath = extractionfolder + "\\" + csvFileName + ".csv";
                    try
                    {
                        File.Create(filePath).Close();
                    }
                    catch (IOException)
                    {
                        string mess = "Creation of file " + csvFileName + ".csv" + "did not work";
                        RhinoApp.WriteLine(mess);
                    }

                    // Creating new string array for printing lines
                    StringBuilder newsb = new StringBuilder();

                    newsb.AppendLine($"{o.Name ?? o.Id}:");
                    File.AppendAllText(filePath, newsb.ToString());

                    var series = o.Data.Select(kvp => kvp.Value);
                    List<string> lines = new List<string>();

                    var m = series.ToArray()[6];

                    StringBuilder sbline = new StringBuilder();
                    string line1 = "Time" + delimiter + $"\t{m.Name}";
                    lines.Add(line1);
                    string line2 = $"\t" + $"\t{m.Units}";
                    for (int i = 0; i < m.Data.Count; i++)
                    {
                        string line = i + delimiter + m.Data[i].ToString() ;
                        lines.Add(line);
                    }
                    File.AppendAllLines(filePath, lines);
                }
            }
            return Result.Success;
        }
    }
}
