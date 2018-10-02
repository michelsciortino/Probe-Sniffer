using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Core.Models;
using Core.Models.Database;

namespace Ui.Controls.DevicesMap
{
    /// <summary>
    /// Logica di interazione per DevicesMap.xaml
    /// </summary>
    public partial class DevicesMap : UserControl
    {
        Collection<UIElement> ESPCollection = new Collection<UIElement>();
        Collection<UIElement> DevicesCollection = new Collection<UIElement>();

        public DevicesMap()
        {
            InitializeComponent();
        }

        #region Ui Elements Constructors

        private UIElement New_ESP(ESP32_Device esp)
        {
            return new TextBlock
            {
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                Text = "\xE704",
                FontSize = 12,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Foreground = ESPsColor,
                ToolTip = New_Device_ToolTip(esp.MAC, esp.X_Position.ToString(), esp.Y_Position.ToString())
            };
        }

        private UIElement New_Device(Probe probe)
        {
            Grid container = new Grid();
            Border background = new Border();
            background.Background = new SolidColorBrush(Color.FromRgb(32,32,32));
            background.BorderThickness = new Thickness(0);
            background.CornerRadius = new CornerRadius(2);
            background.Margin = new Thickness(2.5,.75,2,.75);
            TextBlock t = new TextBlock();
            t.Margin = new Thickness(0);
            t.Padding = new Thickness(0);
            t.Text = "\xE8EA";
            t.FontSize = 12;
            t.FontFamily = new FontFamily("Segoe MDL2 Assets");
            t.Foreground = DevicesColor;
            t.ToolTip = New_Device_ToolTip(probe.Sender.MAC, probe.Sender.X_Position.ToString(), probe.Sender.Y_Position.ToString(), probe.SSID);
            container.Children.Add(background);
            container.Children.Add(t);
            return container;
        }

        private ToolTip New_Device_ToolTip(string MAC, string x, string y,string SSID="")
        {
            StackPanel TextContainer = new StackPanel();
            TextBlock.SetForeground(TextContainer, Brushes.White);

            StackPanel MacContainer = new StackPanel();
            MacContainer.Orientation = Orientation.Horizontal;
            MacContainer.Children.Add(new TextBlock
            {
                Background = Brushes.Transparent,
                Text = "MAC:",
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = Brushes.White
            });
            MacContainer.Children.Add(new TextBlock
            {
                Background = Brushes.Transparent,
                Text = MAC,
                Foreground = Brushes.White
            });
            TextContainer.Children.Add(MacContainer);

            StackPanel CoordinatesContainer = new StackPanel();
            CoordinatesContainer.Orientation = Orientation.Horizontal;
            CoordinatesContainer.Children.Add(new TextBlock
            {
                Text = "x:",
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = Brushes.White
            });
            CoordinatesContainer.Children.Add(new TextBlock
            {
                Text = x,
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = Brushes.White
            });
            CoordinatesContainer.Children.Add(new TextBlock
            {
                Text = "y:",
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = Brushes.White
            });
            CoordinatesContainer.Children.Add(new TextBlock
            {
                Text = y,
                Foreground = Brushes.White
            });
            TextContainer.Children.Add(CoordinatesContainer);

            if (SSID != "")
            {
                StackPanel SSIDContainer = new StackPanel();
                SSIDContainer.Orientation = Orientation.Horizontal;
                SSIDContainer.Children.Add(new TextBlock
                {
                    Background = Brushes.Transparent,
                    Text = "SSID:",
                    Margin = new Thickness(0, 0, 5, 0),
                    Foreground = Brushes.White
                });
                SSIDContainer.Children.Add(new TextBlock
                {
                    Background = Brushes.Transparent,
                    Text = SSID,
                    Foreground = Brushes.White
                });
                TextContainer.Children.Add(SSIDContainer);
            }

            return new ToolTip
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                BorderBrush = Brushes.Transparent,
                Background = Brushes.Transparent,
                Content = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(192, 0, 0, 0)),
                    Padding = new Thickness(7),
                    Margin = new Thickness(0),
                    BorderThickness = Margin,
                    BorderBrush = Brushes.Transparent,
                    CornerRadius = new CornerRadius(7),
                    Child = TextContainer
                }
            };
        }
        #endregion

        #region Properties
        public ObservableCollection<ESP32_Device> ESPs
        {
            get { return (ObservableCollection<ESP32_Device>)GetValue(ESPsProperty); }
            set { SetValue(ESPsProperty, value); }
        }

        public ObservableCollection<Probe> Probes
        {
            get { return (ObservableCollection<Probe>)GetValue(ProbesProperty); }
            set { SetValue(ProbesProperty, value); }
        }

        public double MapHeight
        {
            get { return (double)GetValue(MapHeightProperty); }
            set { SetValue(MapHeightProperty, value); }
        }

        public double MapWidth
        {
            get { return (double)GetValue(MapWidthProperty); }
            set { SetValue(MapWidthProperty, value); }
        }

        public SolidColorBrush ESPsColor
        {
            get { return (SolidColorBrush)GetValue(ESPsColorProperty); }
            set { SetValue(ESPsColorProperty, value); }
        }

        public SolidColorBrush DevicesColor
        {
            get { return (SolidColorBrush)GetValue(DevicesColorProperty); }
            set { SetValue(DevicesColorProperty, value); }
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty ESPsProperty =
            DependencyProperty.Register("ESPs", typeof(ObservableCollection<ESP32_Device>), typeof(DevicesMap), new FrameworkPropertyMetadata(null, OnMapChanged));
        public static readonly DependencyProperty ProbesProperty =
            DependencyProperty.Register("Probes", typeof(ObservableCollection<Probe>), typeof(DevicesMap), new FrameworkPropertyMetadata(null, OnMapChanged));
        public static readonly DependencyProperty MapHeightProperty =
            DependencyProperty.Register("MapHeight", typeof(double), typeof(DevicesMap), new PropertyMetadata((double)200));
        public static readonly DependencyProperty MapWidthProperty =
            DependencyProperty.Register("MapWidth", typeof(double), typeof(DevicesMap), new PropertyMetadata((double)200));
        public static readonly DependencyProperty ESPsColorProperty =
             DependencyProperty.Register("ESPsColor", typeof(SolidColorBrush), typeof(DevicesMap), new PropertyMetadata(Brushes.Black));
        public static readonly DependencyProperty DevicesColorProperty =
            DependencyProperty.Register("DevicesColor", typeof(SolidColorBrush), typeof(DevicesMap), new PropertyMetadata(Brushes.Black));
        #endregion

        #region Private Methods
        private static void OnMapChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DevicesMap map = sender as DevicesMap;

            if (e.OldValue is ObservableCollection<ESP32_Device> old_ESPs)
                old_ESPs.CollectionChanged -= map.OnESPsCollectionChanged;
            if (e.NewValue is ObservableCollection<ESP32_Device> new_ESPs)
                new_ESPs.CollectionChanged += map.OnESPsCollectionChanged;

            if (e.OldValue is ObservableCollection<Probe> old_)
                old_.CollectionChanged -= map.OnDevicesCollectionChanged;
            if (e.NewValue is ObservableCollection<Probe> new_)
                new_.CollectionChanged += map.OnDevicesCollectionChanged;

            map.ESPCollection.Clear();
            if (map.ESPs != null)
            {
                foreach (ESP32_Device esp in map.ESPs)
                {
                    UIElement ESP = map.New_ESP(esp);
                    Canvas.SetLeft(ESP, esp.X_Position);
                    Canvas.SetBottom(ESP, esp.Y_Position);
                    map.ESPCollection.Add(ESP);
                    if (map.MapHeight < esp.Y_Position + 20) map.MapHeight = esp.Y_Position + 20;
                    if (map.MapWidth < esp.X_Position + 20) map.MapWidth = esp.X_Position + 20;
                }
            }
            map.DevicesCollection.Clear();
            if (map.Probes != null)
            {
                foreach (Probe p in map.Probes)
                {
                    UIElement ESP = map.New_Device(p);
                    Canvas.SetLeft(ESP, p.Sender.X_Position);
                    Canvas.SetBottom(ESP, p.Sender.Y_Position);
                    map.DevicesCollection.Add(ESP);
                }
            }
            map.UpdateMap();

        }

        private void OnESPsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ESPCollection.Clear();
            if (e.NewItems != null)
            {
                foreach (ESP32_Device esp in ESPs)
                {
                    UIElement ESP = New_ESP(esp);
                    Canvas.SetLeft(ESP, esp.X_Position);
                    Canvas.SetBottom(ESP, esp.Y_Position);
                    ESPCollection.Add(ESP);
                    if (MapHeight < esp.Y_Position) MapHeight = esp.Y_Position;
                    if (MapWidth < esp.X_Position) MapWidth = esp.X_Position;
                }
            }
            UpdateMap();
        }

        private void OnDevicesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DevicesCollection.Clear();
            if (e.NewItems != null)
            {
                foreach (Probe p in Probes)
                {
                    UIElement ESP = New_Device(p);
                    Canvas.SetLeft(ESP, p.Sender.X_Position);
                    Canvas.SetBottom(ESP, p.Sender.Y_Position);
                    DevicesCollection.Add(ESP);
                }
            }
            UpdateMap();
        }

        private void UpdateMap()
        {
            DevicesContainer.Children.Clear();
            foreach (UIElement el in DevicesCollection)
                DevicesContainer.Children.Add(el);
            foreach (UIElement el in ESPCollection)
                DevicesContainer.Children.Add(el);
        }

        #endregion
    }
}
