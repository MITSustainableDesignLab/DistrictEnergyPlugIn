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

        [DefaultValue(0.18)]
        public double LossesTransmission { get; set; } = 0.18;

        [DefaultValue(0.18)]
        public double LossesHeatHydronic { get; set; } = 0.18;

        [DefaultValue(0.18)]
        public double EfficPowerGen { get; set; } = 0.18;

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
