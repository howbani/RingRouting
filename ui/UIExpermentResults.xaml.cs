using RingRouting.Charts;
using RingRouting.Intilization;
using RingRouting.ExpermentsResults;
using System.Collections.Generic;
using System.Windows;

namespace RingRouting.ui
{
    /// <summary>
    /// Interaction logic for ExpermentResults.xaml
    /// </summary>
    public partial class UIExpermentResults : Window 
    {
        public UIExpermentResults() 
        {
            InitializeComponent();
            /*
            List<CompareResult> list = new List<CompareResult>();
            CompareResult res = CompareResultsClass.GetEnergyDelayForAnExperment();
            list.Add(res);
            dg_data.ItemsSource = list;*/
        }
    }
}
