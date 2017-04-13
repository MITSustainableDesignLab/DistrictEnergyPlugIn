using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Mit.Umi.RhinoServices;
using System.Windows.Forms;
using System.Linq;

namespace ExportLoadsToFile
{
    [System.Runtime.InteropServices.Guid("6fc2fb7f-9bc9-47bd-846e-44c1789273ef")]
    public class ExportLoadColumns : Command
    {
        static ExportLoadColumns _instance;
        private const string delimiter = ",";

        public ExportLoadColumns()
        {
            _instance = this;
        }

        ///<summary>The only instance of the ExportLoadColumns command.</summary>
        public static ExportLoadColumns Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "ExportLoadColumns"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Form1 formData = new Form1();

            if (GlobalContext.ActiveProjectSettings.GenerateHourlyEnergyResults)
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

                // Getting the data
                List<string> line1 = new List<string>();
                List<string> lines = new List<string>();

                StringBuilder sbline = new StringBuilder();
                string firstline = "" + delimiter + "Hour" + delimiter + "Building Name" + delimiter;
                foreach (var o in GlobalContext.GetObjects())
                {
                    var series = o.Data.Select(kvp => kvp.Value);
                    DateTime t = new DateTime(2017, 1, 1, 0, 0, 0);
                    foreach (var m in series)
                    {
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
                                firstline += $"\t{m.Name}" + delimiter;

                                lines.Add($"\t{i}" + delimiter + $"\t{t}" + delimiter + $"\t{o.Name}" + delimiter);
                                lines[i] += m.Data[i].ToString() + delimiter;
                                
                                t = t.AddHours(1);
                            }
                        }
                    }
                }
                // Writing the data in the file
                line1.Add(firstline);
                File.AppendAllLines(file_name, line1);
                File.AppendAllLines(file_name, lines);

            }


            // TODO: complete command.
            return Result.Success;
        }
    }
}
