using System;
using System.Windows.Media;

namespace Ui.Styles
{
    public static class Colors
    {
        private static int next = -1;
        private static Random random = new Random(DateTime.Now.Millisecond);

        public static SolidColorBrush[] palette =
        {
            new SolidColorBrush(Color.FromRgb(236,236,52)),
            new SolidColorBrush(Color.FromRgb(41,185,50)),
            new SolidColorBrush(Color.FromRgb(116,41,157)),
            new SolidColorBrush(Color.FromRgb(236,57,52)),
            new SolidColorBrush(Color.FromRgb(101,205,45)),
            new SolidColorBrush(Color.FromRgb(44,94,154)),
            new SolidColorBrush(Color.FromRgb(218,48,85)),
            new SolidColorBrush(Color.FromRgb(236,163,52)),
            new SolidColorBrush(Color.FromRgb(31,141,141)),
            new SolidColorBrush(Color.FromRgb(78,50,162)),
            new SolidColorBrush(Color.FromRgb(236,116,52)),
            new SolidColorBrush(Color.FromRgb(174,174,174))
        };

        public static SolidColorBrush Random()
        {
            return palette[random.Next() % palette.Length];
        }
        public static SolidColorBrush Next
        {
            get
            {
                next = (next + 1) % palette.Length;
                return palette[next];
            }
        }
    }
}
