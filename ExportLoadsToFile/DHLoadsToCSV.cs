using System;
using Rhino;
using Rhino.Commands;
using Mit.Umi.RhinoServices;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;

namespace ExportLoadsToFile
{
    [System.Runtime.InteropServices.Guid("bfa8ed0d-0388-42fa-859a-8bcae50314a7")]
    public class DHLoadsToCSV : Command
    {
        static DHLoadsToCSV _instance;
        private const string delimiter = ",";
        public DHLoadsToCSV()
        {
            _instance = this;
        }

        ///<summary>This command is used to export a the loads to a CSV format in the C:\UMI\temp\ folder for use with the District Heating plugin.</summary>
        public static DHLoadsToCSV Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DHLoadsToCSV"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //Create the file
            string file_name = @"C:\UMI\temp\Loads.csv";
            using (var writer = new StreamWriter(file_name))
            using (var csvWriter = new CsvWriter(writer))
            {
                var filterList = new List<string>();
                filterList.Add("SDL/Lighting");
                filterList.Add("SDL/Equipment");
                filterList.Add("SDL/Heating");
                filterList.Add("SDL/Cooling");
                filterList.Add("SDL/Domestic Hot Water");

                int count = 0;
                foreach (var o in GlobalContext.GetObjects())
                {
                    int t = 1;
                    var series = o.Data.Select(kvp => kvp.Value)
                        .Where(item => filterList.Contains(item.Name));
                    for (int i = 0; i < 8760; i++)
                    {
                        //Setup the header line. Count makes this part run only once.
                        if (count < 1)
                        {
                            csvWriter.WriteField("Hour");
                            csvWriter.WriteField("Building");
                            foreach (var m in series)
                                csvWriter.WriteField(m.Name);
                            csvWriter.NextRecord();
                            count++;
                        }

                        //Write all the fields
                        csvWriter.WriteField(t);
                        t++;
                        csvWriter.WriteField(o.Name);
                        foreach (var m in series)
                            csvWriter.WriteField(m.Data[i]);
                        csvWriter.NextRecord();
                    }
                }
                writer.Close();
            }

            // TODO: complete command.
            return Result.Success;
        }
    }
}
