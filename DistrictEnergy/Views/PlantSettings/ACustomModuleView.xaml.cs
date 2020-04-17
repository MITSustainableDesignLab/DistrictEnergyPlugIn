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
using DistrictEnergy.ViewModels;

namespace DistrictEnergy.Views.PlantSettings
{
    /// <summary>
    /// Interaction logic for ACustomModuleView.xaml
    /// </summary>
    public partial class ACustomModuleView : UserControl
    {
        public ACustomModuleView()
        {
            InitializeComponent();
            DataContext = new ACustomModuleViewModel();
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                ((Panel) this.Parent).Children.Remove(this);
                // Todo: Implement Cleanup Logic Here
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}