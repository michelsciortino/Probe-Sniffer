using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Configurator
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowState state;
        private double oldT, oldL, oldW, oldH;
        public MainWindow()
        {
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
            App.Current.Shutdown();
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
