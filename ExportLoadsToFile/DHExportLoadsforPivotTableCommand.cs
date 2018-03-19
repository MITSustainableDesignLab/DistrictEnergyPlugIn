using System;
using Rhino.Commands;
using Rhino;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Umi.RhinoServices.Context;

namespace ExportLoadsToFile
{
    [System.Runtime.InteropServices.Guid("b49322ba-d330-4be8-938c-dcbd46096d34")]
    public class DHExportLoadsforPivotTableCommand : Command
    {
        public DHExportLoadsforPivotTableCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static DHExportLoadsforPivotTableCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "DHExportLoadsforPivotTable"; }
        }

        public string savePath { get; private set; }
        public bool more { get; private set; }
        public object ExtraSeries { get; set; }

        private const string delimiter = ",";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Form1 formData = new Form1();

            // This command generates csv files for hourly simulated loads
            if (UmiContext.Current.ProjectSettings.GenerateHourlyEnergyResults != false)
            {
                
                var file_name = "";
                SaveFileDialog save_file_dialog = new SaveFileDialog();
                save_file_dialog.Filter = "Comma Separated Value | *.csv";
                save_file_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                save_file_dialog.RestoreDirectory = false;
                save_file_dialog.AddExtension = true;
                save_file_dialog.Title = "Choose a file name";

                if (save_file_dialog.ShowDialog() == DialogResult.OK)
                {
                    file_name = save_file_dialog.FileName;
                }

                // Create file and test if already exists
                try
                {
                    File.Create(file_name).Close();
                }
                catch (IOException)
                {
                    string mess = "Creation of file did not work";
                    RhinoApp.WriteLine(mess);
                    return Result.Failure;
                }

                // If file exists, open form
                if (File.Exists(file_name) == true)
                {
                    Application.Run(formData);
                }
                else
                {
                    RhinoApp.WriteLine("An error occured while creating the file, please try again in another folder");
                    return Result.Failure;
                }

                // What if the form is closed by the user (Red X button)
                if (formData.count == 0)
                {
                    RhinoApp.WriteLine("Command canceled");
                    return Result.Cancel;
                }

                // Everything is fine, let's continue
                // Getting the data
                List<string> line1 = new List<string>();
                List<string> lines = new List<string>();

                StringBuilder sbline = new StringBuilder();
                string firstline = "Building ID" + delimiter + "Building Name" + delimiter + "Series Name" + delimiter + "Time" + delimiter + "Values";
                line1.Add(firstline);
                File.AppendAllLines(file_name, line1);
                foreach (var o in UmiContext.Current.GetObjects())
                {
                    var series = o.Data.Select(kvp => kvp.Value);

                    foreach (var m in series)
                    {
                        DateTime t = new DateTime(2017, 1, 1, 0, 0, 0);
                        for (int i = 0; i < m.Data.Count; i++)
                        {
                            var name = m.Name;
                            if (String.IsNullOrWhiteSpace(name) == true)
                                name = "";
                            var unit = m.Units;
                            if (String.IsNullOrWhiteSpace(unit) == true)
                                unit = "No unit";
                            var timestep = m.Resolution;
                            if (String.IsNullOrWhiteSpace(timestep) == true)
                                timestep = "";
                            //else
                            //    timestep = "- " + timestep.ToString();
                            string row = name.ToString() + "[" + unit.ToString() + "]" + timestep;

                            if (formData.Data1.Contains(row))
                            {
                                var line = $"\t{o.Id}" + delimiter + $"\t{o.Name}" + delimiter + $"\t{m.Name}" + delimiter + t.ToString() + delimiter + m.Data[i].ToString(); 

                                lines.Add(line);
                                t = t.AddHours(1);
                            }
                        }
                    }
                }
                // Writing the data in the file
                File.AppendAllLines(file_name, lines);

            }
            else
            {
                RhinoApp.WriteLine("Error: An hourly simulation needs to be performed first");
                return Result.Failure;
            }
            // ---
            RhinoApp.WriteLine("Done");
            return Result.Success;
        }
    }
}
