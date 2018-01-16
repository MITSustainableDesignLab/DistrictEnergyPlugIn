using System;
using Rhino;
using Rhino.Commands;
using EnergyPlusWeather;
using System.IO;
using CsvHelper;
using System.Linq;
using Mit.Umi.RhinoServices.Context;

namespace ExportLoadsToFile
{
    [System.Runtime.InteropServices.Guid("97543640-7c91-46d5-b7bb-15209db82fd2")]
    public class DHDryBulbCSV : Command
    {
        static DHDryBulbCSV _instance;
        public DHDryBulbCSV()
        {
            _instance = this;
        }

        ///<summary>The only instance of the DHDryBulbCSV command.</summary>
        public static DHDryBulbCSV Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "DHDryBulbCSV"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string filepath = UmiContext.Current.WeatherFilePath;
            EPWeatherData epw = new EPWeatherData();
            epw.GetRawData(filepath);
            string[] paramsOfInterest = new string[1];
            paramsOfInterest[0] = "DB";
            epw.GetWeatherStats(paramsOfInterest);
            var r = epw.GetHourlyListsTransformed(paramsOfInterest.ToList());

            //Write file
            // Create file and test if already exists
            string file_name = @"C:\UMI\temp\DryBulbData.csv";
            try
            {
                StreamWriter writer = File.CreateText(file_name);
                var csv = new CsvWriter(writer);
                csv.WriteField("Hour");
                csv.WriteField("DB");
                csv.NextRecord();
                foreach (var item in r)
                {
                    DateTime time = new DateTime(2017, 1, 1, 0, 0, 0);
                    foreach (var line in item.Value)
                    {
                        csv.WriteField(time);
                        csv.WriteField(line);
                        csv.NextRecord();
                        time = time.AddHours(1);
                    }
                    //// these extra lines shouldn't exist but Brad's script needs a loop value (first of the year) at
                    //// the end of the file
                    //DateTime time2 = new DateTime(2017, 1, 1, 0, 0, 0);
                    //csv.WriteField(time2);
                    //csv.WriteField(item.Value[0]);
                    //csv.NextRecord();
                }

                writer.Close();
            }
            catch (IOException)
            {
                string mess = "Creation of file did not work";
                RhinoApp.WriteLine(mess);
                throw new ApplicationException("File can't be created");
            }
            return Result.Success;
        }
    }
}
