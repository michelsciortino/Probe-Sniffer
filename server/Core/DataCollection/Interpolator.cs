using Core.Models;
using System;
using System.Collections.Generic;
using static System.Math;
using System.Windows;

namespace Core.DataCollection
{
    public static class Interpolator
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
            Point[] best_sol = null;
            bool got_values = true;
            bool gotFirstCouple = false;
            bool gotSecondCouple = false;

            if (packetDetections.Count < 2)
                throw new ArgumentException("At least two detection needed; packetDetection contains " + packetDetections.Count + " detections");

            //We apply a % reducion to the db mesurament values to find the best value of the device position
            for (double redux = 1; redux > 0 && got_values is true; redux -= 0.1)
            {
                List<Point> pointsList = new List<Point>();
                gotFirstCouple = false;
                gotSecondCouple = false;

                for (int i = 0; i < packetDetections.Count && got_values is true; i++)
                {
                    for (int j = i + 1; j < packetDetections.Count && got_values is true; j++)
                    {
                        Point[] points = InterpolateTwoDetections(packetDetections[i], packetDetections[j], redux);
                        if (points is null)
                        {
                            got_values = false;
                            break;
                        }
                        AddAndCheck(points, pointsList, ref gotFirstCouple, ref gotSecondCouple);
                    }
                }
                if (got_values is false) break;
                best_sol = pointsList.ToArray();
            }

            if (best_sol == null) return default;

            return GetMidPoint(best_sol);
        }

        #endregion

        #region Private Methods
        private static Point[] InterpolateTwoDetections(KeyValuePair<ESP32_Device, int> first, KeyValuePair<ESP32_Device, int> second, double redux)
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
            exp = (27.55 - (20 * Math.Log10(2412)) + Math.Abs(first.Value) * redux) / 20.0;
            double radius1 = Math.Pow(10.0, exp);
            exp = (27.55 - (20 * Math.Log10(2412)) + Math.Abs(second.Value) * redux) / 20.0;
            double radius2 = Math.Pow(10.0, exp);

            Circumference c1 = new Circumference(radius1, first.Key.X_Position, first.Key.Y_Position);
            Circumference c2 = new Circumference(radius2, second.Key.X_Position, second.Key.Y_Position);

            return Circumference.GetCircumferencesIntersections(c1, c2);
        }

        private static void AddAndCheck(Point[] newPoints, List<Point> pointsList, ref bool gotFirstCouple, ref bool gotSecondCouple)
        {
            double min = double.MaxValue;

            if (gotFirstCouple is false)
            {
                pointsList.AddRange(newPoints);
                gotFirstCouple = true;
            }
            else
            {
                if (gotSecondCouple is false)
                {
                    double distance = 0;
                    Point[] pointsNear = new Point[2];
                    foreach (Point newPoint in newPoints)
                    {
                        distance = (Sqrt(Pow(newPoint.X - pointsList[0].X, 2) + Pow(newPoint.Y - pointsList[0].Y, 2)));
                        if (distance < min)
                        {
                            min = distance;
                            pointsNear[0] = pointsList[0];
                            pointsNear[1] = newPoint;
                        }
                        distance = (Sqrt(Pow(newPoint.X - pointsList[1].X, 2) + Pow(newPoint.Y - pointsList[1].Y, 2)));
                        if (distance < min)
                        {
                            min = distance;
                            pointsNear[0] = pointsList[1];
                            pointsNear[1] = newPoint;
                        }
                    }
                    pointsList.Clear();
                    pointsList.AddRange(pointsNear);
                    gotSecondCouple = true;
                    return;
                }
                else
                {
                    double distance = 0;
                    Point pointNear = new Point();
                    foreach (Point couple in newPoints)
                    {
                        foreach (Point point in pointsList)
                        {
                            distance = (Sqrt(Pow(couple.X - point.X, 2) + Pow(couple.Y - point.Y, 2)));
                            if (distance < min)
                            {
                                min = distance;
                                pointNear.X = couple.X;
                                pointNear.Y = couple.Y;
                            }
                        }
                    }
                    pointsList.Add(pointNear);
                    return;
                }
            }
        }

        private static Point GetMidPoint(Point[] points)
        {
            double sumX = 0, sumY = 0, X = 0, Y = 0;
            foreach (Point couple in points)
            {
                sumX += couple.X;
                sumY += couple.Y;
            }
            X = sumX / points.Length;
            Y = sumY / points.Length;
            return new Point(X, Y);
        }
        #endregion

        #region Private Classes
        /// <summary>
        /// Class used to interpolate
        /// </summary>
        private class Circumference
        {
            #region Properties
            public double Radius { get; private set; }
            public double Xcenter { get; private set; }
            public double Ycenter { get; private set; }
            #endregion

            #region Constructor
            public Circumference(double radius, double Xcenter, double Ycenter)
            {
                this.Radius = radius;
                this.Xcenter = Xcenter;
                this.Ycenter = Ycenter;
            }
            #endregion

            public static Point[] GetCircumferencesIntersections(Circumference c1, Circumference c2)
            {
                Point[] points = new Point[2];
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

                    points[0] = new Point(X1, Y1);
                    points[1] = new Point(X2, Y2);

                    return points;
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion
    }
}