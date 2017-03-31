using Mit.Umi.RhinoServices;
using System;
using System.ComponentModel;

namespace DistrictEnergy.ViewModels
{
    public class DistrictSettingsViewModel : INotifyPropertyChanged
    {
 
        private DistrictSettings backing = new DistrictSettings();

        public DistrictSettingsViewModel()
        {
            GlobalContext.ActiveProjectSwitched += (s, e) => PopulateFrom(e.NewProject);
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private void PopulateFrom(DistrictEnergyPlugIn panel)
        {
            if (panel == null) { return; }
            if (panel.activeSettings == null)
            {
                throw new ArgumentException("A project settings viewmodel cannot be instantiated from a project with no instantiated settings object");
            }

            backing = panel.activeSettings;
            PropertyChanged(this, new PropertyChangedEventArgs(String.Empty));
        }

        public double ElectricityGenerationCost
        {
            get { return backing.ElectricityGenerationCost; }
            set
            {
                backing.ElectricityGenerationCost = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElectricityGenerationCost)));
            }
        }

        public double PriceNaturalGas
        {
            get { return backing.PriceNaturalGas; }
            set
            {
                backing.PriceNaturalGas = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriceNaturalGas)));
            }
        }

        public double EmissionsElectricGeneration
        {
            get { return backing.EmissionsElectricGeneration; }
            set
            {
                backing.EmissionsElectricGeneration = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EmissionsElectricGeneration)));
            }
        }

        public double LossesTransmission
        {
            get { return backing.LossesTransmission; }
            set
            {
                backing.LossesTransmission = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesTransmission)));
            }
        }

        public double LossesHeatHydronic
        {
            get { return backing.LossesHeatHydronic; }
            set
            {
                backing.LossesHeatHydronic = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LossesHeatHydronic)));
            }
        }

        public double EfficPowerGen
        {
            get { return backing.EfficPowerGen; }
            set
            {
                backing.EfficPowerGen = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EfficPowerGen)));
            }
        }

    }
}
