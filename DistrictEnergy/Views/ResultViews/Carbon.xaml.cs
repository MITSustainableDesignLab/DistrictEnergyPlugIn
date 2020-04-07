using System.Windows.Controls;
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views.ResultViews
{
    /// <summary>
    /// Interaction logic for Carbon.xaml
    /// </summary>
    public partial class Carbon : UserControl
    {
        public Carbon()
        {
            InitializeComponent();

            DataContext = new CarbonViewModel();
        }
    }
}