using System.Windows.Controls;
using DistrictEnergy.Networks.Loads;
using DistrictEnergy.Networks.ThermalPlants;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views
{
    /// <summary>
    /// Interaction logic for ExportView.xaml
    /// </summary>
    public partial class ExportView : UserControl
    {
        public ExportView(Exportable export)
        {
            InitializeComponent();
            DataContext = new ExportViewModel(export);
        }
    }
}
