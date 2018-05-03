using ProbeSniffer.ViewModels.DataVisualizer;
using System.Windows.Controls;

namespace ProbeSniffer.Views.DataVisualizer
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
