using System;
using System.Linq;
using DistrictEnergy.Networks.ThermalPlants;

namespace DistrictEnergy.ViewModels
{
    class ACustomModuleViewModel : PlantSettingsViewModel
    {
        public ACustomModuleViewModel()
        {
            Instance = this;
        }

        public new static ACustomModuleViewModel Instance { get; set; }

        public Guid Id { get; set; }

        public String Name
        {
            get
            {
                try
                {
                    return DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>()
                        .First(x => x.Id == Id).Name;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return "Unammed";
                }
            }
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>().First(x => x.Id == Id)
                    .Name = value;
                OnPropertyChanged();
            }
        }

        public double F
        {
            get
            {
                try
                {
                    return DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>()
                        .First(x => x.Id == Id).F;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return 0;
                }
            }
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>().First(x => x.Id == Id)
                    .F = value;
                OnPropertyChanged();
            }
        }

        public double V
        {
            get
            {
                try
                {
                    return DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>()
                        .First(x => x.Id == Id).V;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return 0;
                }
            }
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>().First(x => x.Id == Id)
                    .V = value;
                OnPropertyChanged();
            }
        }

        public double Norm
        {
            get
            {
                try
                {
                    return DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>()
                        .First(x => x.Id == Id).Norm * 100;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return 0;
                }
            }
            set
            {
                DistrictControl.Instance.ListOfPlantSettings.OfType<CustomEnergySupplyModule>().First(x => x.Id == Id)
                    .Norm = value / 100;
                OnPropertyChanged();
            }
        }
    }
}