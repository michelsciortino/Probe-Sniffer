using ProbeSniffer.ViewModels.DataVisualizer;
using System.Windows.Controls;

namespace ProbeSniffer.Views.DataVisualizer
{
    /// <summary>
    /// Logica di interazione per HiddenDevicesView.xaml
    /// </summary>
    public partial class HiddenDevicesView : Page
    {
        public HiddenDevicesView(HiddenDevicesViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
