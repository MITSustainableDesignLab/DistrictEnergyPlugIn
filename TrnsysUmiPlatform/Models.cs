using System;

namespace TrnsysUmiPlatform
{ 
    public class TrnsysModel
    {
        /// <summary>
        /// A TrnsysModel sets information
        /// </summary>
        /// <param name="modelname">The name of the Project</param>
        /// <param name="hourlytimestep">The Timestep</param>
        /// <param name="weather"></param>
        /// <param name="projectcreator"></param>
        /// <param name="description"></param>
        /// <param name="directory"></param>
        public TrnsysModel(string modelname, int hourlytimestep, string weather, string projectcreator, string description, string directory)
        {
            ModelName = modelname;
            HourlyTimestep = hourlytimestep;
            WeatherFile = weather;
            ProjectCreator = projectcreator;
            CreationDate = DateTime.Now.ToString();
            ModifiedDate = DateTime.Now.ToString();
            Description = description;
            WorkingDirectory = directory;
        }

        public string ModelName { get; set; }
        public double HourlyTimestep { get; set; }
        public string PlantSelection { get; set; }
        public string WeatherFile { get; set; }
        public string ProjectCreator { get; set; }
        public string CreationDate { get; set; }
        public string ModifiedDate { get; set; }
        public string Description { get; set; }
        public string WorkingDirectory { get; set; }
    }
}
