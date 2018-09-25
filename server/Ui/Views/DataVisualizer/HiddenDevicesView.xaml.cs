using Ui.ViewModels.DataVisualizer;
using System.Windows.Controls;

namespace Ui.Views.DataVisualizer
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
