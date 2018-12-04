using Core.Models;
using System;
using System.Collections.Generic;
using System.Windows;
namespace Core.DataCollection
{
    public class Interpolator2
    {
        #region Public Methods
        /// <summary>
        /// Find a point given a list of devices and a SignalStrength
        /// </summary>
        /// <param name="packetDetections">List of pair of ESP32_Device and detected SignalStrength(Db)</param>
        /// <returns>Return intersection Point</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="packetDetections"/> contains less than 2 packet detections</exception>
        /// <exception cref="ArithmeticException">Thrown if at least two intersecting circumferences have not been found</exception>
        public static Point Interpolate(List<KeyValuePair<ESP32_Device, int>> packetDetections)
        {
            if (packetDetections.Count < 2)
                throw new ArgumentException("At least two detection needed; packetDetection contains " + packetDetections.Count + " detections");
			int den = 0;

            Point p = new Point { X = 0, Y = 0 };
			
			foreach(var pair in packetDetections)
            {
                p.X += pair.Key.X_Position * (100 - Math.Abs(pair.Value));
                p.Y += pair.Key.Y_Position * (100 - Math.Abs(pair.Value));
                den += 100 - Math.Abs(pair.Value);
            }
            p.X /= den;
            p.Y /= den;
            return p;
        }

        #endregion
    }
}
