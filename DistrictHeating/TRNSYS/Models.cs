using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistrictEnergy.TRNSYS
{
    class TrnsysModel
    {
        public TrnsysModel()
        {
            ModelName = "Default Name";
            HourlyTimestep = 1;
            PlantSelection = "Plant 1";
            WeatherFile = "CAN_PQ_Montreal.Intl.AP.716270_CWEC";
            ProjectCreator = "God";
            CreationDate = DateTime.Now.ToString();
            ModifiedDate = DateTime.Now.ToString();
            Description = "Description goes here";
        }


        public string ModelName { get; set; }
        public double HourlyTimestep { get; set; }
        public string PlantSelection { get; set; }
        public string WeatherFile { get; set; }
        public string ProjectCreator { get; set; }
        public string CreationDate { get; set; }
        public string ModifiedDate { get; set; }
        public string Description { get; set; }
    }
}
