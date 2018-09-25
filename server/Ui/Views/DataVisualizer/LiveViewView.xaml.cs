using Ui.ViewModels.DataVisualizer;
using System.Windows.Controls;

namespace Ui.Views.DataVisualizer
{
    /// <summary>
    /// Logica di interazione per LiveViewPage.xaml
    /// </summary>
    public partial class LiveViewView : Page
    {
        public LiveViewView(LiveViewViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
