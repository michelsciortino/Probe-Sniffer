using Core.Models;
using System;
using System.Collections.Generic;
using static System.Math;
using System.Windows;

namespace Core.DataCollection
{
    class Interpolator
    {
        #region Public Method
        /// <summary>
        /// Find a point given a list of devices and a SignalStrength
        /// </summary>
        /// <param name="packetDetection">List of Device with SignalStrength(Db)</param>
        /// <returns>Return intersection Point</returns>
        public static Point Interpolates(List<KeyValuePair<ESP32_Device, int>> packetDetection)
        {
            bool firstCouple = false;
            Point point = new Point();
            Point[] firstPoints = new Point[2];
            Point[] points = new Point[2];
            List<Point> totalPoints = new List<Point>();

            for (int i = 0; i < packetDetection.Count; i++)
            {
                for (int j = i + 1; j < packetDetection.Count; j++)
                {
                    points = InterpolatesTwoDevice(packetDetection[i], packetDetection[j]);
                    if (points is null)
                        continue;
                    AddAndCheck(totalPoints, firstPoints, points, ref firstCouple);
                }
            }
            point = MediumPoints(totalPoints);

            return point;
        }

        #endregion
       
        #region Private Method
        private static Point[] InterpolatesTwoDevice(KeyValuePair<ESP32_Device, int> first, KeyValuePair<ESP32_Device, int> second)
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

            Circumference c1 = new Circumference(radius1, first.Key.X_Position, first.Key.Y_Position);
            Circumference c2 = new Circumference(radius2, second.Key.X_Position, second.Key.Y_Position);

            return TwoCircumferenceIntersection(c1, c2);
        }

        private static Point[] TwoCircumferenceIntersection(Circumference c1, Circumference c2)
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
                Console.WriteLine("X1:{0}   Y1:{1}\nX2:{2}    Y2:{3}", Round(X1, 3), Round(Y1, 3), Round(X2, 3), Round(Y2, 3));     //stampo con tre cifre significative

                points[0] = new Point(X1, Y1);
                points[1] = new Point(X2, Y2);

                return points;
            }
            else
            {
                return null;
            }
        }

        private static void AddAndCheck(List<Point> totalPoints, Point[] firstPoints, Point[] points, ref bool firstCouple)
        {
            double min = 100;

            if (firstCouple is false)
            {
                firstPoints[0] = points[0];
                firstPoints[1] = points[1];
                firstCouple = true;
            }
            else
            {
                if (totalPoints.Count == 0)
                {
                    double distance = 0;
                    Point[] pointsNear = new Point[2];
                    foreach (Point couple in points)
                    {
                        distance = (Sqrt(Pow(couple.X - firstPoints[0].X, 2) + Pow(couple.Y - firstPoints[0].Y, 2)));
                        if (distance < min)
                        {
                            min = distance;
                            pointsNear[0] = firstPoints[0];
                            pointsNear[1] = couple;
                        }
                        distance = (Sqrt(Pow(couple.X - firstPoints[1].X, 2) + Pow(couple.Y - firstPoints[1].Y, 2)));
                        if (distance < min)
                        {
                            min = distance;
                            pointsNear[0] = firstPoints[1];
                            pointsNear[1] = couple;
                        }
                    }
                    totalPoints.AddRange(pointsNear);
                    return;
                }
                else
                {
                    double distance = 0;
                    Point pointNear = new Point();
                    foreach (Point couple in points)
                    {
                        foreach (Point point in totalPoints)
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
                    totalPoints.Add(pointNear);
                    return;
                }
            }
        }

        private static Point MediumPoints(List<Point> totalPoint)
        {
            double sumX = 0, sumY = 0, X = 0, Y = 0;
            foreach (Point couple in totalPoint)
            {
                sumX += couple.X;
                sumY += couple.Y;
            }
            X = sumX / totalPoint.Count;
            Y = sumY / totalPoint.Count;
            Point point = new Point(X, Y);
            return point;
        }
        #endregion

        #region Class Used
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
        }
        #endregion
    }
}