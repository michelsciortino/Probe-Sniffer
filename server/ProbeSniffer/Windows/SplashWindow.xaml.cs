using ProbeSniffer.ViewModels;
using System.Windows;

namespace ProbeSniffer.Windows
{
    /// <summary>
    /// Logica di interazione per SplashView.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        public SplashWindow(SplashViewModel splashViewModel)
        {
            DataContext = splashViewModel;
            InitializeComponent();
        }
    }
}
