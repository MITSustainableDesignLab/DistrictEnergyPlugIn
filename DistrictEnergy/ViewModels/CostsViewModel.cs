using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using DistrictEnergy.Annotations;
using DistrictEnergy.Networks.ThermalPlants;
using LiveCharts;
using LiveCharts.Wpf;
using Umi.RhinoServices.Context;
using Umi.RhinoServices.UmiEvents;

namespace DistrictEnergy.ViewModels
{
    internal class CostsViewModel : INotifyPropertyChanged
    {
        private double _totalCost = 0;
        private double _normalizedTotalCost = 1;

        public CostsViewModel()
        {
            SeriesCollection = new SeriesCollection();
            Labels = new[] {"Natural Gas", "Grid Electricity"};
            Formatter = delegate(double value)
            {
                if (Math.Abs(value) > 999999) return string.Format("{0:N} M$", value / 1000000);
                return string.Format("{0:N0} $", value);
            };
            TotalCost = 0;

            UmiEventSource.Instance.ProjectOpened += SubscribeEvents;
            UmiEventSource.Instance.ProjectClosed += OnProjectClosed;

            CostLabelPointFormatter = delegate(ChartPoint chartPoint)
            {
                if (Math.Abs(chartPoint.Y) > 999999) return string.Format("{0:N3} M$", chartPoint.Y / 1000000);
                return string.Format("{0:N2} $", chartPoint.Y);
            };
        }

        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }
        public Func<ChartPoint, string> CostLabelPointFormatter { get; set; }

        public double TotalCost
        {
            get => _totalCost;
            set
            {
                _totalCost = value;
                OnPropertyChanged();
            }
        }

        public SeriesCollection SeriesCollection { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnProjectClosed(object sender, EventArgs e)
        {
            SeriesCollection.Clear();
        }

        private void SubscribeEvents(object sender, UmiContext e)
        {
            DHSimulateDistrictEnergy.Instance.ResultsArray.ResultsChanged += UpdateCostsChart;
        }

        private void UpdateCostsChart(object sender, EventArgs e)
        {
            if (DHSimulateDistrictEnergy.Instance == null) return;
            var instance = DHSimulateDistrictEnergy.Instance;
            SeriesCollection.Clear();

            var resultsArrayChwAbs = instance.ResultsArray.ChwAbs;
            var absChillerCost =
                resultsArrayChwAbs.Max() * ChilledWaterViewModel.Instance.F_ECH * Settings.AnnuityFactor +
                resultsArrayChwAbs.Sum() * ChilledWaterViewModel.Instance.V_ECH;

            if (absChillerCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "Absorption Chiller",
                    Values = new ChartValues<double>
                    {
                        absChillerCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(146, 241, 254)),
                });
            }

            var resultsArrayChwEch = instance.ResultsArray.ChwEch;
            var chillerCost = resultsArrayChwEch.Max() * ChilledWaterViewModel.Instance.F_ECH * Settings.AnnuityFactor +
                              resultsArrayChwEch.Sum() * ChilledWaterViewModel.Instance.V_ECH;

            if (chillerCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "Electric Chiller",
                    Values = new ChartValues<double>
                    {
                        chillerCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(93, 153, 170)),
                });
            }

            var resultsArrayElecPv = instance.ResultsArray.ElecPv;
            var pvCost = resultsArrayElecPv.Max() * ElectricGenerationViewModel.Instance.F_PV * Settings.AnnuityFactor +
                         resultsArrayElecPv.Sum() * ElectricGenerationViewModel.Instance.V_PV;

            if (pvCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "PV",
                    Values = new ChartValues<double>
                    {
                        pvCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(112, 159, 15)),
                });
            }

            var resultsWind = instance.ResultsArray.ElecWndUsed;
            var windCost = resultsWind.Max() * ElectricGenerationViewModel.Instance.F_WND * Settings.AnnuityFactor +
                           resultsWind.Sum() * ElectricGenerationViewModel.Instance.V_WND;

            if (windCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "Wind",
                    Values = new ChartValues<double>
                    {
                        windCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(20, 120, 15)),
                });
            }

            var resultsEhp = instance.ResultsArray.HwEhp;
            var ehpCost = resultsEhp.Max() * HotWaterViewModel.Instance.F_EHP * Settings.AnnuityFactor +
                          resultsEhp.Sum() * HotWaterViewModel.Instance.V_EHP;

            if (ehpCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "Heat Pump",
                    Values = new ChartValues<double>
                    {
                        ehpCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(231, 71, 126)),
                });
            }

            var resultShw = instance.ResultsArray.HwShw;
            var shwCost = resultShw.Max() * HotWaterViewModel.Instance.F_SHW * Settings.AnnuityFactor +
                          resultShw.Sum() * HotWaterViewModel.Instance.V_SHW;

            if (shwCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "Solar Thermal",
                    Values = new ChartValues<double>
                    {
                        shwCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(251, 209, 39)),
                });
            }

            var resultHwt = instance.ResultsArray.HwHwt;
            var hwtCost = resultHwt.Max() * HotWaterViewModel.Instance.F_HWT * Settings.AnnuityFactor +
                          resultHwt.Sum() * HotWaterViewModel.Instance.V_HWT;

            if (hwtCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "Hot Water Storage",
                    Values = new ChartValues<double>
                    {
                        hwtCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(253, 199, 204)),
                });
            }

            var resultNgb = instance.ResultsArray.HwNgb;
            var ngbCost = resultNgb.Max() * HotWaterViewModel.Instance.F_NGB * Settings.AnnuityFactor +
                          resultNgb.Sum() * HotWaterViewModel.Instance.V_NGB;

            if (ngbCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "NG Boiler",
                    Values = new ChartValues<double>
                    {
                        ngbCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(189, 133, 74)),
                });
            }

            var resultsBat = instance.ResultsArray.ElecBat;
            var batCost = resultsBat.Max() * ElectricGenerationViewModel.Instance.F_BAT * Settings.AnnuityFactor +
                          resultsBat.Sum() * ElectricGenerationViewModel.Instance.V_BAT;

            if (batCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "Battery",
                    Values = new ChartValues<double>
                    {
                        batCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(192, 244, 66)),
                });
            }

            var resultsArrayChp = CombinedHeatAndPowerViewModel.Instance.TMOD_CHP == TrakingModeEnum.Electrical
                ? instance.ResultsArray.ElecChp
                : instance.ResultsArray.HwChp;
            var chpCost = resultsArrayChp.Max() * CombinedHeatAndPowerViewModel.Instance.F * Settings.AnnuityFactor +
                          resultsArrayChp.Sum() * CombinedHeatAndPowerViewModel.Instance.V;

            if (chpCost > 0)
            {
                SeriesCollection.Add(new StackedColumnSeries
                {
                    Title = "CHP Plant",
                    Values = new ChartValues<double>
                    {
                        chpCost
                    },
                    LabelPoint = CostLabelPointFormatter,
                    Fill = new SolidColorBrush(Color.FromRgb(253, 199, 204))
                });
            }

            var gasCost = instance.ResultsArray.NgasProj.Sum() * UmiContext.Current.ProjectSettings.GasDollars;
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = "Natural Gas",
                Values = new ChartValues<double>
                {
                    gasCost
                },
                LabelPoint = CostLabelPointFormatter
            });
            var electricityCost = instance.ResultsArray.ElecProj.Sum() *
                                  UmiContext.Current.ProjectSettings.ElectricityDollars;
            SeriesCollection.Add(new StackedColumnSeries
            {
                Title = "Grid Electricity",
                Values = new ChartValues<double>
                {
                    electricityCost
                },
                LabelPoint = CostLabelPointFormatter,
                Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            });
            TotalCost = electricityCost + gasCost + chillerCost + absChillerCost + pvCost + windCost + ehpCost +
                        shwCost + hwtCost + ngbCost + batCost + chpCost;

            NormalizedTotalCost = TotalCost / FloorArea;
        }

        public double NormalizedTotalCost
        {
            get => _normalizedTotalCost;
            set
            {
                _normalizedTotalCost = value;
                OnPropertyChanged();
            }
        }

        private double FloorArea
        {
            get { return UmiContext.Current.Buildings.All.Select(b => b.GrossFloorArea).Sum().Value; }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}