using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LiveCharts;
using LiveCharts.Wpf;
using Mit.Umi.RhinoServices.Context;
using Mit.Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    public class ResultsViewModel
    {
        public ResultsViewModel()
        {
            Instance = this;
            UmiEventSource.Instance.ProjectOpened += CreateChart;
        }

        private void CreateChart(object sender, UmiContext context)
        {
            var instance = DHSimulateDistrictEnergy.Instance;
            var hwShw = instance.HW_SHW;
            var hwHwt = instance.HW_HWT;
            var hwAbs = instance.HW_ABS;
            Series = new SeriesCollection
            {
                new StackedAreaSeries
                {
                    Values = new ChartValues<double>(hwShw.AsEnumerable()),
                    Title = "Solar Hot Water",
                    LineSmoothness = 0
                },
                new StackedAreaSeries
                {
                    Values = new ChartValues<double>(hwHwt.AsEnumerable()),
                    Title = "Hot water Tank",
                    LineSmoothness = 0
                },
                new StackedAreaSeries
                {
                    Values = new ChartValues<double>(hwAbs.AsEnumerable()),
                    Title = "Absorption Chiller",
                    LineSmoothness = 0
                }
            };
        }

        public static ResultsViewModel Instance { get; set; }
        public static SeriesCollection Series { get; set; }
    }
}