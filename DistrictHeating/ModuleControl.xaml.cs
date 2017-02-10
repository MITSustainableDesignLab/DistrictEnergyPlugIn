using Rhino;
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

namespace DistrictEnergy
{
    /// <summary>
    /// Interaction logic for ModuleControl.xaml
    /// </summary>
    public partial class ModuleControl : UserControl
    {
        public ModuleControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("CreateNetworkLayer", echo: true);
        }

        private void button_Click_1(object sender, RoutedEventArgs e)
        {
            RhinoApp.RunScript("RunTrnsys", echo: true);
        }
    }
}
