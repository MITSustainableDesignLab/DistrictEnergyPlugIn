using System.ComponentModel;

namespace DistrictEnergy
{
    public class DistrictSettings : INotifyPropertyChanged
    {
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

        private double _electricityGeneration;
        [DefaultValue(0.15)]
        public double ElectricityGenerationCost
        {
            get
            {
                return _electricityGeneration;
            }
            set
            {
                _electricityGeneration = value;
                OnPropertyChanged("ElectricityGenerationCost");
            }
        }

        private double _priceNaturalGas;
        [DefaultValue(0.18)]
        public double PriceNaturalGas
        {
            get
            {
                return _priceNaturalGas;
            }
            set
            {
                _priceNaturalGas = value;
                OnPropertyChanged("PriceNaturalGas");
            }
        }

        private double _emissionsElectricGeneration;
        [DefaultValue(0.18)]
        public double EmissionsElectricGeneration
        {
            get
            {
                return _emissionsElectricGeneration;
            }
            set
            {
                _emissionsElectricGeneration = value;
                OnPropertyChanged("EmissionsElectricGeneration");
            }
        }

        private double _lossesTransmission;
        [DefaultValue(0.18)]
        public double LossesTransmission
        {
            get
            {
                return _lossesTransmission;
            }
            set
            {
                _lossesTransmission = value;
                OnPropertyChanged("LossesTransmission");
            }
        }

        private double _lossesHeatHydronic;
        [DefaultValue(0.18)]
        public double LossesHeatHydronic
        {
            get
            {
                return _lossesHeatHydronic;
            }
            set
            {
                _lossesHeatHydronic = value;
                OnPropertyChanged("LossesHeatHydronic");
            }
        }

        private double _efficPowerGen;
        [DefaultValue(0.18)]
        public double EfficPowerGen
        {
            get
            {
                return _efficPowerGen;
            }
            set
            {
                _efficPowerGen = value;
                OnPropertyChanged("EfficPowerGen");
            }
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }

    internal class DistrictSettingsPath
    {
        public const string SettingsFilePathInBundle = "districtSettings.json";
    }
}
