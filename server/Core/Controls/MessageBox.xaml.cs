using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Core.Controls
{
    /// <summary>
    /// Logica di interazione per MessageBox.xaml
    /// </summary>
    public partial class MessageBox : Window
    {
        private MessageBoxResult _result = MessageBoxResult.None;
        private string _message = "",_caption = "";
        public MessageBoxResult Result => _result;
        public MessageBox(string messageBoxText, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon =0, MessageBoxResult defaultResult= MessageBoxResult.None)
        {
            InitializeComponent();
            switch (button)
            {
                case MessageBoxButton.OK:
                    OkButton.Visibility = Visibility.Visible;
                    OkButton.Focus();
                    break;
                case MessageBoxButton.OKCancel:
                    OkButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    CancelButton.Focus();
                    break;
                case MessageBoxButton.YesNo:
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    NoButton.Focus();
                    break;
                case MessageBoxButton.YesNoCancel:
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    CancelButton.Focus();
                    break;
            }


            if (icon != 0)
            {
                /*SystemIconConverter converter = new SystemIconConverter();
                MessageIcon.Source = (BitmapSource)converter.Convert(null, null, icon.ToString(), null);
                if (MessageIcon.Source != null) MessageIcon.Visibility = Visibility.Visible;*/
                switch (icon)
                {
                    case MessageBoxImage.Error:
                        MessageIcon.Text = "\xEA39";
                        MessageIcon.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxImage.Exclamation:
                        MessageIcon.Text = "\xE783";
                        MessageIcon.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxImage.Information:
                        MessageIcon.Text = "\xE946";
                        MessageIcon.Visibility = Visibility.Visible;
                        break;
                    case MessageBoxImage.None:
                        MessageIcon.Text = null;
                        break;
                    case MessageBoxImage.Question:
                        MessageIcon.Text = "\xF142";
                        MessageIcon.Visibility = Visibility.Visible;
                        break;
                }
            }

            _result = defaultResult;
            Message.Text = messageBoxText;
            Caption.Text = caption;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            Height = WindowContent.DesiredSize.Height;
            Width = WindowContent.DesiredSize.Width;
            Top = SystemParameters.WorkArea.Height / 2 - Height / 2;
            Left = SystemParameters.WorkArea.Width / 2 - Width / 2;
            //Height = Caption.DesiredSize.Height + Body.DesiredSize.Height + Body.Margin.Top + Body.Margin.Bottom + Footer.DesiredSize.Height;
        }


        public MessageBoxResult Show()
        { 
            ShowDialog();
            return _result;
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.Cancel;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.No;
            Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.Yes;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.OK;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        [ValueConversion(typeof(string), typeof(BitmapSource))]
        public class SystemIconConverter : IValueConverter
        {
            public object Convert(object value, Type type, object parameter, CultureInfo culture)
            {
                Icon icon = (Icon)typeof(SystemIcons).GetProperty(parameter.ToString(), BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                return bs;
            }

            public object ConvertBack(object value, Type type, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }
    }
}
