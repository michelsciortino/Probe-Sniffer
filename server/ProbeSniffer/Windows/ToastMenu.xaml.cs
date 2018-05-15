using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// 
    public partial class ToastMenu : Window
    {
        public event RoutedEventHandler ExitCLicked = (sender, e) => { };
        public event RoutedEventHandler ShowGraphClicked = (sender, e) => { };

        #region Private Members
        private Duration duration;
        private DoubleAnimation fadeIn, slideUp;
        #endregion

        public ToastMenu()
        {            
            InitializeComponent();
            duration = new Duration(new TimeSpan(0, 0, 0, 0, 40));
        }
        
        private void window_Activated(object sender, EventArgs e)
        {
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

        private void Exit_button_Click(object sender, RoutedEventArgs e)
        {
            ExitCLicked(this,null);
        }

        private void ShowGraph_button_click(object sender, RoutedEventArgs e)
        {
            ShowGraphClicked(this, null);
        }

    }
}