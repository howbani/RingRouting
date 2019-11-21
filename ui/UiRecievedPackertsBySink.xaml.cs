using RingRouting.Dataplane;
using System.Windows;

namespace RingRouting.ui
{
    /// <summary>
    /// Interaction logic for UiRecievedPackertsBySink.xaml
    /// </summary>
    public partial class UiRecievedPackertsBySink : Window
    {
       
        public UiRecievedPackertsBySink()
        {
            InitializeComponent();
            dg_packets.ItemsSource = PublicParameters.FinishedRoutedPackets;
        }
    }
    
}
