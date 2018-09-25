using Ui.ViewModels.DataVisualizer;
using System.Windows.Controls;

namespace Ui.Views.DataVisualizer
{
    /// <summary>
    /// Logica di interazione per StatisticsView.xaml
    /// </summary>
    public partial class StatisticsView : Page
    {
        public StatisticsView(StatisticsViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
