using Core.ViewModelBase;
using Server.ViewModels;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Server.Windows
{
    /// <summary>
    /// Logica di interazione per Log.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        private WindowState state;
        private double oldT, oldL, oldW, oldH;

        public LogWindow(BaseViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            Stream iconStream = Assembly.GetAssembly(typeof(Core.Controls.MessageBox)).GetManifestResourceStream("Core.Resources.icon.ico");
            Icon = new BitmapImage();
            (Icon as BitmapImage).BeginInit();
            (Icon as BitmapImage).StreamSource = iconStream;
            (Icon as BitmapImage).EndInit();
            WindowState = WindowState.Normal;
            state = WindowState;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                    Maximize(null, null);
                else
                    DragMove();
            }
        }

        public void Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void Minimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        public void Maximize(object sender, RoutedEventArgs e)
        {
            switch (state)
            {
                case WindowState.Normal:
                    BorderThickness = new Thickness(0);
                    oldW = Width;
                    oldH = Height;
                    oldT = Top;
                    oldL = Left;
                    Top = 0;
                    Left = 0;
                    Height = SystemParameters.WorkArea.Height;
                    Width = SystemParameters.WorkArea.Width;
                    state = WindowState.Maximized;
                    break;

                case WindowState.Maximized:
                    BorderThickness = new Thickness(1);
                    state = WindowState.Normal;
                    Width = oldW;
                    Height = oldH;
                    Top = oldT;
                    Left = oldL;
                    break;
            }
        }
    }
}
