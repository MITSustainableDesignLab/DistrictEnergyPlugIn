using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using DistrictEnergy.Annotations;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views.ResultViews
{
    /// <summary>
    /// Interaction logic for Costs.xaml
    /// </summary>
    public partial class Costs : UserControl
    {
        public Costs()
        {
            InitializeComponent();
            DataContext = new CostsViewModel();
        }
    }
}