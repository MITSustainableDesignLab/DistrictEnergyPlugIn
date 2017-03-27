using System;
using Rhino;
using Rhino.Commands;
using System.Diagnostics;
using System.IO;
using CsvHelper;
using EnergyPlusWeather;
using System.Linq;
using Mit.Umi.RhinoServices;

namespace DistrictEnergy
{
    [System.Runtime.InteropServices.Guid("5597dafc-25d7-44ce-863d-00859ef8bb66")]
    public class EnergySupplyScheme : Command
    {
        static EnergySupplyScheme _instance;
        public EnergySupplyScheme()
        {
            _instance = this;
        }

        ///<summary>The only instance of the EnergySupplyScheme command.</summary>
        public static EnergySupplyScheme Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "EnergySupplyScheme"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                return Result.Success;
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine($"Error: {e.Message}");
                return Result.Failure;
            }
        }

        public class cmd
        {
            string a;
            public static void runcmd(string fileName, string args)
            {
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo(@"C:\Python27\python.exe", fileName)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = fileName + args,
                };

                p.ErrorDataReceived += cmd_Error;
                p.OutputDataReceived += cmd_DataReceived;
                p.EnableRaisingEvents = true;

                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                p.WaitForExit();

            }
            static void cmd_DataReceived(object sender, DataReceivedEventArgs e)
            {
                //RhinoApp.WriteLine("Output from other process");
                RhinoApp.WriteLine(e.Data);
            }

            static void cmd_Error(object sender, DataReceivedEventArgs e)
            {
                //RhinoApp.WriteLine("Error from other process");
                RhinoApp.WriteLine(e.Data);
            }
        }

        private static void DryBulbCSV()
        {
            string filepath = GlobalContext.ActiveEpwPath;
            EPWeatherData epw = new EPWeatherData();
            epw.GetRawData(filepath);
            string[] paramsOfInterest = new string[1];
            paramsOfInterest[0] = "DB";
            epw.GetWeatherStats(paramsOfInterest);
            var r = epw.GetHourlyListsTransformed(paramsOfInterest.ToList());

            //Write file
            // Create file and test if already exists
            string file_name = @"C:\tmp\DryBulbData.csv";
            try
            {
                StreamWriter writer = File.CreateText(file_name);
                var csv = new CsvWriter(writer);
                foreach (var item in r)
                {
                    foreach (var line in item.Value)
                    {
                        int time = 1;
                        csv.WriteField(time);
                        csv.WriteField(line);
                        csv.NextRecord();
                        time++;
                    }
                }
                writer.Close();
            }
            catch (IOException)
            {
                string mess = "Creation of file did not work";
                RhinoApp.WriteLine(mess);
                throw new ApplicationException("File can't be created");
            }
        }

    }
}
