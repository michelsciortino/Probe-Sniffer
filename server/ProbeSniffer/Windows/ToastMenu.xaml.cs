using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProbeSniffer.Windows
{
    /// <summary>
    /// Logica di interazione per ToastMenu.xaml
    /// </summary>
    public partial class ToastMenu : Window
    {
        private Duration duration;
        private DoubleAnimation fadeIn, slideUp;

        public ToastMenu()
        {
            InitializeComponent();
            duration = new Duration(new TimeSpan(0, 0, 0, 0, 40));
        }
        private void window_Activated(object sender, EventArgs e)
        {
            Left = SystemParameters.WorkArea.Width - Width;
            fadeIn = new DoubleAnimation(0, 1, duration, FillBehavior.HoldEnd);
            slideUp = new DoubleAnimation(SystemParameters.WorkArea.Height, SystemParameters.WorkArea.Height - Height, duration, FillBehavior.HoldEnd);
            fadeIn.AccelerationRatio = 0;
            fadeIn.DecelerationRatio = 1;
            fadeIn.SpeedRatio = 0.1;
            slideUp.AccelerationRatio = 0;
            slideUp.DecelerationRatio = 1;
            slideUp.SpeedRatio = 0.1;
            this.BeginAnimation(OpacityProperty, fadeIn);
            this.BeginAnimation(TopProperty, slideUp);
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            Opacity = 0;
        }
    }
}
