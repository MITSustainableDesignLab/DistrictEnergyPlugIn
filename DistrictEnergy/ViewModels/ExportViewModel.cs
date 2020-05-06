using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using DistrictEnergy.Annotations;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    public class ExportViewModel : INotifyPropertyChanged
    {
        private Color _color;
        private double _f;

        public ExportViewModel(Exportable plant)
        {
            Plant = plant;
        }

        public Exportable Plant { get; set; }

        public string Name => Plant.Name;

        public Color Color
        {
            get => Plant.Fill[Plant.OutputType].Color;
            set
            {
                if (value.Equals(_color)) return;
                _color = value;
                OnPropertyChanged();
                Plant.Fill[Plant.OutputType] = new SolidColorBrush(_color);
            }
        }

        public double F
        {
            get => Plant.F;
            set
            {
                if (value.Equals(_f)) return;
                _f = value;
                OnPropertyChanged();
                Plant.F = value;
            }
        }

        public double V
        {
            get => Plant.V;
            set
            {
                if (value.Equals(_f)) return;
                _f = value;
                OnPropertyChanged();
                Plant.V = value;
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