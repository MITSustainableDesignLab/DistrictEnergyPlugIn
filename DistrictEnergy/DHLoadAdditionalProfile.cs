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
using Rhino.Input;
using Rhino.Input.Custom;
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

            var lt = LoadType.Cooling;
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

            using (var getOptions = new GetOption())
            {
                for (;;)
                {
                    getOptions.SetCommandPrompt("What type of load profile is this");
                    getOptions.ClearCommandOptions();
                    var modeInt = AddEnumOptionList(getOptions, lt);

                    if (getOptions.Get() == GetResult.Option)
                    {
                        if (getOptions.Option().Index == modeInt)
                            lt = RetrieveEnumOptionValue<LoadType>(getOptions.Option().CurrentListOptionIndex);
                    }
                    else
                    {
                        //Read the contents of the file into the umi db
                        var records = AddAdditionalLoad(filePath, context, lt);

                        RhinoApp.WriteLine(fileContent, "Additional Load at path: " + filePath);
                        return Result.Success;
                    }
                }
            }
        }

        private static int AddEnumOptionList(GetOption getOptions, LoadType lt)
        {
            var type = lt.GetType();

            var names = Enum.GetNames(type);
            var current = Enum.GetName(type, lt);

            var location = Array.IndexOf(names, current);
            if (location == -1)
                throw new ArgumentException("enumerationCurrent is not an existing value");
            return getOptions.AddOptionList(type.Name, names, location);
        }

        public static T RetrieveEnumOptionValue<T>(int resultIndex)
        {
            var type = typeof(T);

            if (!type.IsEnum)
                throw new ApplicationException("T must be enum");

            var values = Enum.GetValues(type);
            var current = values.GetValue(resultIndex);

            return (T) current;
        }

        private static ICollection<IUmiObject> AddAdditionalLoad(string filePath, UmiContext context, LoadType loadType)
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
                        Data = new Dictionary<string, UmiDataSeries>(),
                        FilePath = filePath,
                        LoadType = loadType
                    };
                    record.Data["Additional " + loadType] = new UmiDataSeries
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
        public string FilePath { get; set; }
        public LoadType LoadType { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public IDictionary<string, UmiDataSeries> Data { get; set; }
    }

    public enum LoadType
    {
        Cooling = 1,
        Heating = 2,
        Elec = 3
    }

}