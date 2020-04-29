using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using DistrictEnergy.Annotations;
using DistrictEnergy.Networks.Loads;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    class AnAdditionalLoadViewModel : INotifyPropertyChanged
    {
        public AnAdditionalLoadViewModel()
        {
            Instance = this;
        }

        public static AnAdditionalLoadViewModel Instance { get; set; }

        public Guid Id { get; set; }

        public String Name
        {
            get
            {
                try
                {
                    return DistrictControl.Instance.ListOfDistrictLoads.OfType<AdditionalLoads>()
                        .First(x => x.Id == Id).Name;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return "Unnamed";
                }
            }
            set
            {
                DistrictControl.Instance.ListOfDistrictLoads.OfType<AdditionalLoads>().First(x => x.Id == Id)
                    .Name = value;
                OnPropertyChanged();
            }
        }

        public Color Color
        {
            get
            {

                return DistrictControl.Instance.ListOfDistrictLoads.OfType<AdditionalLoads>()
                    .First(x => x.Id == Id).Color;
                
            }
            set
            {
                DistrictControl.Instance.ListOfDistrictLoads.OfType<AdditionalLoads>().First(x => x.Id == Id)
                    .Color = value;
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