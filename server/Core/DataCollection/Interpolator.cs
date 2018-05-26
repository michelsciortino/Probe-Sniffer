using Core.Models;
using System;
using System.Collections.Generic;
using static System.Math;

namespace Core.DataCollection
{
    class Interpolator
    {
        #region Public Method
        /// <summary>
        /// Find a point given a list of devices and a SignalStrength
        /// </summary>
        /// <param name="deviceList">List of Device with SignalStrength(Db)</param>
        /// <returns>Return intersection point</returns>
        public static ICollection<KeyValuePair<double, double>> Interpolates(List<KeyValuePair<ESP32_Device, int>> deviceList)
        {
            bool firstCouple = false;
            bool first = false;
            ICollection<KeyValuePair<double, double>> point = new Dictionary<double, double>();
            List<KeyValuePair<double, double>> twoIntersect = new List<KeyValuePair<double, double>>();
            List<KeyValuePair<double, double>> totalPoint = new List<KeyValuePair<double, double>>();
            List<KeyValuePair<double, double>> firstPoint = new List<KeyValuePair<double, double>>();

            for (int i = 0; i < deviceList.Count; i++)
            {
                for (int j = i + 1; j < deviceList.Count; j++)
                {
                    twoIntersect = InterpolatesTwoDevice(deviceList[i], deviceList[j]);
                    if (firstCouple is false)
                    {
                        firstPoint.Add(twoIntersect[0]);
                        firstPoint.Add(twoIntersect[1]);
                        firstCouple = true;
                        first = true;
                    }
                    else
                    {
                        totalPoint.AddRange(CheckPoint(twoIntersect, totalPoint, firstPoint, first));
                        first = false;
                    }
                }
            }

            point = MediumPoints(totalPoint);

            return point;
        }

        #endregion
       
        #region Private Method
        private static List<KeyValuePair<double, double>> InterpolatesTwoDevice(KeyValuePair<ESP32_Device, int> first, KeyValuePair<ESP32_Device, int> second)
        {
            //find distance from Db ---> radius
            //Distance(km) = 10(Free Space Path Loss – 32.44 – 20log10(f)) / 20       MHz e Km
            //Distance(m) = 10^(Free Space Path Loss + 27.55 – 20log10(f))/20         MHz e m
            //fspl = (Pt/Pr)*Gt*Gr
            //cellular gain in or out --> 2.2dBi        power=variable 16/12 (2.4GHz)
            //ESP power = 20.5 dBm

            //distance = 10 ^ ((27.55 - (20 * log10(frequency)) + signalLevel)/20)
            //double exp = (27.55 - (20 * Math.log10(freqInMHz)) + Math.abs(signalLevelInDb)) / 20.0;
            //return Math.pow(10.0, exp);

            double exp = 0.0;
            exp = (27.55 - (20 * Math.Log10(2412)) + Math.Abs(first.Value)) / 20.0;
            double radius1 = Math.Pow(10.0, exp);
            exp = (27.55 - (20 * Math.Log10(2412)) + Math.Abs(second.Value)) / 20.0;
            double radius2 = Math.Pow(10.0, exp);
            //radius, xcenter, ycenter
            Circumference c1 = new Circumference(radius1, first.Key.X_Position, first.Key.Y_Position);
            Circumference c2 = new Circumference(radius2, second.Key.X_Position, second.Key.Y_Position);

            List<KeyValuePair<double, double>> twoIntersect = new List<KeyValuePair<double, double>>();

            twoIntersect = TwoCircumferenceIntersection(c1, c2);

            return twoIntersect;
        }

        private static List<KeyValuePair<double, double>> TwoCircumferenceIntersection(Circumference c1, Circumference c2)
        {
            List<KeyValuePair<double, double>> twoIntersect = new List<KeyValuePair<double, double>>();

            double xc1 = c1.Xcenter, yc1 = c1.Ycenter, xc2 = c2.Xcenter, yc2 = c2.Ycenter, r1 = c1.Radius, r2 = c2.Radius;

            double D = Sqrt(Pow(xc2 - xc1, 2) + Pow(yc2 - yc1, 2));

            if (r1 + r2 > D)
            {
                double rad = Sqrt((D + r1 + r2) * (D + r1 - r2) * (D - r1 + r2) * (-D + r1 + r2));
                double delta = (1.0 / 4.0) * rad;

                double x_cost = (xc1 + xc2) / 2.0 + (((xc2 - xc1) * (Pow(r1, 2) - Pow(r2, 2))) / (2 * Pow(D, 2)));
                double x_variable = 2.0 * delta * (yc1 - yc2) / Pow(D, 2);
                double X1 = x_cost + x_variable;
                double X2 = x_cost - x_variable;

                double y_cost = (yc1 + yc2) / 2.0 + (((yc2 - yc1) * (Pow(r1, 2) - Pow(r2, 2))) / (2 * Pow(D, 2)));
                double y_variable = 2.0 * delta * (xc1 - xc2) / Pow(D, 2);
                double Y1 = y_cost - y_variable;
                double Y2 = y_cost + y_variable;
                //Console.WriteLine("X1:{0}   Y1:{1}\nX2:{2}    Y2:{3}", Round(X1, 3), Round(Y1, 3), Round(X2, 3), Round(Y2, 3));     //stampo con tre cifre significative

                twoIntersect.Add(new KeyValuePair<double, double>(X1, Y1));

                twoIntersect.Add(new KeyValuePair<double, double>(X2, Y2));

                return twoIntersect;
            }
            else
            {
                return null;
            }
        }

        private static List<KeyValuePair<double, double>> CheckPoint(List<KeyValuePair<double, double>> twoIntersect, List<KeyValuePair<double, double>> totalPoint, List<KeyValuePair<double, double>> firstPoint, bool first)
        {
            double min = 100;
            List<KeyValuePair<double, double>> pointsNear = new List<KeyValuePair<double, double>>();
            if (first == true)
            {
                foreach (KeyValuePair<double, double> couple in twoIntersect)
                {
                    if (Abs(couple.Key - (firstPoint[0].Key)) < min)
                    {
                        min = Abs(couple.Key - (firstPoint[0].Key));
                        pointsNear.Clear();
                        pointsNear.Add(couple);
                        pointsNear.Add(firstPoint[0]);
                    }
                    if (Abs(couple.Key - (firstPoint[1].Key)) < min)
                    {
                        min = Abs(couple.Key - (firstPoint[1].Key));
                        pointsNear.Clear();
                        pointsNear.Add(couple);
                        pointsNear.Add(firstPoint[1]);
                    }
                }
                return pointsNear;
            }
            else
            {
                foreach (KeyValuePair<double, double> couple in twoIntersect)
                {
                    foreach (KeyValuePair<double, double> point in totalPoint)
                    {
                        if (Abs(couple.Key - (point.Key)) < min)
                        {
                            min = Abs(couple.Key - (point.Key));
                            pointsNear.Clear();
                            pointsNear.Add(couple);
                        }
                    }
                }
                return pointsNear;
            }
        }

        private static ICollection<KeyValuePair<double, double>> MediumPoints(List<KeyValuePair<double, double>> totalPoint)
        {
            ICollection<KeyValuePair<double, double>> point = new Dictionary<double, double>();

            double sumX = 0, sumY = 0, X = 0, Y = 0;
            foreach (KeyValuePair<double, double> couple in totalPoint)
            {
                sumX += couple.Key;
                sumY += couple.Value;
            }
            X = sumX / totalPoint.Count;
            Y = sumY / totalPoint.Count;
            point.Add(new KeyValuePair<double, double>(X, Y));
            return point;
        }
        #endregion

        #region Class Used
        /// <summary>
        /// Class used to interpolate
        /// </summary>
        private class Circumference
        {
            #region Constructor
            public Circumference(double radius, double Xcenter, double Ycenter)
            {
                this.Radius = radius;
                this.Xcenter = Xcenter;
                this.Ycenter = Ycenter;
            }
            #endregion
            #region Public Method
            public double Radius { get; }
            public double Xcenter { get; }
            public double Ycenter { get; }
            #endregion

        }
        #endregion
    }
}