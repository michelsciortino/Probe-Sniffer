using ProbeSniffer.ViewModels.DataVisualizer;
using System.Windows.Controls;

namespace ProbeSniffer.Views.DataVisualizer
{
    /// <summary>
    /// Logica di interazione per SliderViewView.xaml
    /// </summary>
    public partial class SliderViewView : Page
    {
        public SliderViewView(SliderViewViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
