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
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;
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

            Result = defaultResult;
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
        }
        
        public new MessageBoxResult Show()
        { 
            ShowDialog();
            return Result;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
