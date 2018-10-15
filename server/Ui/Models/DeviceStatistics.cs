using Core.Models.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.ComponentModel;

namespace Ui.Models
{
    public enum Precision
    {
        Hour,
        Day
    }
    public class DeviceStatistics:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string MAC { get; set; }
        public int Tot_Probes { get; set; }
        private bool _active=true;
        public SolidColorBrush LineColor { get; set; }
        public int[] Probes { get; set; }

        public void SetPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Active
        {
            get { return _active; }
            set
            {
                _active = value;
                SetPropertyChanged(nameof(Active));
            }
        }

        public static List<DeviceStatistics> DoStatistics(List<ProbesInterval> intervals, DateTime start, DateTime end, Precision precision)
        {
            int TotalDays = (int)(end - start).TotalDays;
            int TotalHours = (int)(end - start).TotalHours;
            Dictionary<string, DeviceStatistics> devicesStatistics = new Dictionary<string, DeviceStatistics>();
            intervals = intervals.OrderBy(a => a.Timestamp).ToList();
            foreach (var interval in intervals)
            {
                foreach (var p in interval.Probes)
                {
                    if (!devicesStatistics.ContainsKey(p.Sender.MAC))
                    {
                        DeviceStatistics d = new DeviceStatistics
                        {
                            MAC = p.Sender.MAC,
                            LineColor = Styles.Colors.Next,
                            Active = true,
                            Tot_Probes = 0
                        };
                        if (precision == Precision.Day)
                        {
                            d.Probes = new int[TotalDays];
                            Array.Clear(d.Probes, 0, TotalDays);
                        }
                        else if (precision == Precision.Hour)
                        {
                            d.Probes = new int[TotalHours];
                            Array.Clear(d.Probes, 0, TotalHours);
                        }
                        devicesStatistics.Add(d.MAC, d);
                    }

                    if (precision == Precision.Day)
                        devicesStatistics[p.Sender.MAC].Probes[(int)(p.Timestamp - start).TotalDays] += 1;
                    else if (precision == Precision.Hour)
                        devicesStatistics[p.Sender.MAC].Probes[(int)(p.Timestamp - start).TotalHours] += 1;

                    devicesStatistics[p.Sender.MAC].Tot_Probes += 1;
                }
            }
            return devicesStatistics.Values.ToList();
        }
    }
}
