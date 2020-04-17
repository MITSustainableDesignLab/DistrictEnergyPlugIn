using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistrictEnergy.ViewModels
{
    class ACustomModuleViewModel : PlantSettingsViewModel
    {
        public ACustomModuleViewModel()
        {
            Instance = this;
        }

        public new static ACustomModuleViewModel Instance { get; set; }

        public String Name { get; set; }
    }
}
