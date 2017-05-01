using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;

namespace DistrictEnergy.Metrics
{
    [System.Runtime.InteropServices.Guid("df8e2e9c-61e3-42b4-b7b8-aeb080d8281b")]
    public class AverageDiameterCommand : Command
    {
        static AverageDiameterCommand _instance;
        private Enum sm;

        public AverageDiameterCommand()
        {
            _instance = this;
        }

        ///<summary>The only instance of the AverageDiameterCommand command.</summary>
        public static AverageDiameterCommand Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "AverageDiameterCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            DiamMode sm = DiamMode.SwedishEmpirical;
            using (GetOption getOptions = new GetOption())
            {
                for (;;)
                {
                    getOptions.SetCommandPrompt("Select an average diameter method.");
                    getOptions.ClearCommandOptions();
                    int modeInt = AddEnumOptionList(getOptions, sm);

                    if (getOptions.Get() == Rhino.Input.GetResult.Option)
                    {
                        if(getOptions.Option().Index == modeInt)
                        {
                            sm = RetrieveEnumOptionValue<DiamMode>
                                (getOptions.Option().CurrentListOptionIndex);
                        }
                        continue;
                    }
                    else
                    {
                        var averageDiam = AverageDiameter(sm);
                        RhinoApp.WriteLine("Average Diameter : {0:F2} [m]", averageDiam);
                        return Result.Success;
                    }
                }
            }
            RhinoApp.WriteLine("error?");
            return Result.Cancel;
        }

        public static int AddEnumOptionList(GetOption getOptions, Enum enumerationCurrent)
        {
            Type type = enumerationCurrent.GetType();

            string[] names = Enum.GetNames(type);
            string current = Enum.GetName(type, enumerationCurrent);

            int location = Array.IndexOf(names, current);
            if (location == -1)
                throw new ArgumentException("enumerationCurrent is not an existing value");

            return getOptions.AddOptionList(type.Name, names, location);
        }

        public enum DiamMode
        {
            /// <summary>
            /// Based on the relationship between linear heat densities and average pipe diameters in 134 observations concerning whole or parts of Swedish district heating systems
            /// </summary>
            SwedishEmpirical = 1,
            /// <summary>
            /// Uses Rhino model pipe diameters.
            /// </summary>
            Model = 2,
        }

        public static T RetrieveEnumOptionValue<T>(int resultIndex)
        {
            Type type = typeof(T);

            if (!type.IsEnum)
                throw new ApplicationException("T must be enum");

            Array values = Enum.GetValues(type);
            object current = values.GetValue(resultIndex);

            return (T)current;
        }

        /// <summary>
        /// Calculates the average pipe diameter following a certain method.
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        public static double AverageDiameter(DiamMode sm)
        {
            double averageDiameter;
            switch (sm)
            {
                case DiamMode.SwedishEmpirical:
                    double linearHeatDesnsity = Metrics.HeatSoldPerAnnum() / Metrics.TotalRouteLength();
                    averageDiameter = Metrics.AveragePipeDiamSwedish(linearHeatDesnsity);
                    break;
                case DiamMode.Model:
                    throw new ApplicationException("Method not implemented yet");
                default:
                    throw new ApplicationException("No behaviour is defined for this enum value");
            }
            return averageDiameter;
        }
    }
}
