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
    public partial class Costs : UserControl, INotifyPropertyChanged
    {
        public Costs()
        {
            InitializeComponent();
            DataContext = new CostsViewModel();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}