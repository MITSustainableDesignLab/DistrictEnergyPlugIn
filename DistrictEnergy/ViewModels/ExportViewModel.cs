using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using DistrictEnergy.Annotations;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    public class ExportViewModel : INotifyPropertyChanged
    {

        public ExportViewModel(Exportable plant)
        {
            Plant = plant;
        }

        public Exportable Plant { get; set; }

        public string Name => Plant.Name;

        public Color Color
        {
            get => DistrictControl.Instance.ListOfPlantSettings.First(o => o.Id == Plant.Id).Fill[Plant.OutputType].Color;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.First(o => o.Id == Plant.Id).Fill[Plant.OutputType] = new SolidColorBrush(value);
                OnPropertyChanged();
            }
        }

        public double F
        {
            get => DistrictControl.Instance.ListOfPlantSettings.First(o => o.Id == Plant.Id).F;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.First(o => o.Id == Plant.Id).F = value;
                OnPropertyChanged();
            }
        }

        public double V
        {
            get => DistrictControl.Instance.ListOfPlantSettings.First(o => o.Id == Plant.Id).V;
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.First(o => o.Id == Plant.Id).V = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}