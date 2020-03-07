using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using CsvHelper;
using Rhino;
using Rhino.Commands;
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

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var context = UmiContext.Current;
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = doc.Path;
                openFileDialog.Filter = "Comma Separated Value | *.csv";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    //Read the contents of the file into the umi db
                    var records = AddAdditionalLoad(filePath, context);
                }
            }

            RhinoApp.WriteLine(fileContent, "Additional Load at path: " + filePath);
            return Result.Success;
        }

        private static ICollection<IUmiObject> AddAdditionalLoad(string filePath, UmiContext context)
        {
            ICollection<IUmiObject> records;
            using (var reader = new StreamReader(filePath))
            {
                using (var csv = new CsvReader(reader))
                {
                    records = new Collection<IUmiObject>();
                    var record = new AdditionalLoad
                    {
                        Name = "Additional Load", //Path.GetFileName(filePath),
                        Id = ToGuid(Path.GetFileName(filePath)).ToString(),
                        Data = new Dictionary<string, UmiDataSeries>()
                    };
                    record.Data["Additional Load"] = new UmiDataSeries
                    {
                        Name = "Additional Load",
                        Units = "kWh",
                        Resolution = "Hourly",
                        Data = new List<double>()
                    };
                    csv.Read();
                    csv.ReadHeader();
                    while (csv.Read())
                    {
                        var timestamp = csv.GetField<DateTime>("TimeStamp");
                        var value = csv.GetField<double>("Value");
                        record.Data["Additional Load"].Data.Add(value);
                    }

                    records.Add(record);
                    context.StoreObjects(records);
                }
            }

            return records;
        }

        /// <summary>
        ///     Converts a String to a unique GUID
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Guid ToGuid(string src)
        {
            var stringbytes = Encoding.UTF8.GetBytes(src);
            var hashedBytes = new SHA1CryptoServiceProvider()
                .ComputeHash(stringbytes);
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes);
        }
    }

    public class AdditionalLoad : IUmiObject
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public IDictionary<string, UmiDataSeries> Data { get; set; }
    }
}