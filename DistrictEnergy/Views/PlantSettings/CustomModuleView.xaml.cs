using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DistrictEnergy.Views.PlantSettings
{
    /// <summary>
    /// Interaction logic for CustomModuleView.xaml
    /// </summary>
    public partial class CustomModuleView : UserControl
    {
        public CustomModuleView()
        {
            InitializeComponent();
        }

        private void btnAddCustomEnergyModule_Click(object sender, RoutedEventArgs e)
        {
            ACustomModuleView newBtn = new ACustomModuleView();
            splMain.Children.Add(newBtn);
        }
    }
}
