using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Ui.Controls.PointsGraph
{
    /// <summary>
    /// Logica di interazione per PointGraph.xaml
    /// </summary>
    public partial class MultiPointsGraph : UserControl
    {
        #region Private Members
        private Polyline xAxisLine = null;
        private Polyline yAxisLine = null;
        private Polyline[] lines = null;
        private int maxY = 200, minY = 0;
        #endregion

        #region Constructor
        public MultiPointsGraph()
        {
            InitializeComponent();
        }
        #endregion       
                
        #region Common Controls Creators        
        /// <summary>
        /// Label generator for Canvas containers
        /// </summary>
        /// <param name="text">The text of the label</param>
        /// <param name="x">The left margin from the canvas border</param>
        /// <param name="y">The bottom margin from the canvas border</param>
        /// <param name="foreground">The foreground of the label</param>
        /// <param name="fontSize">The font size of the label</param>
        /// <returns></returns>
        private static Decorator CanvasLabel(string text, Double x, Double y, Brush foreground, double fontSize)
        {
            TextBlock label = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = foreground
            };
            Decorator container = new Viewbox
            {
                Child = label
            };

            Canvas.SetBottom(container, y);
            Canvas.SetLeft(container, x);

            return container;
        }
        

        /// <summary>
        /// Generates a vertical arrow polyline
        /// </summary>
        /// <param name="vertexPoint">The vertex point of the arrow</param>
        /// <param name="size">The arrow size</param>
        /// <returns>The arrow</returns>
        private static Polyline VerticalArrow(Point vertexPoint, double size)
        {
            Polyline arrow = new Polyline();
            arrow.Points.Add(new Point(vertexPoint.X - size, vertexPoint.Y - size));
            arrow.Points.Add(vertexPoint);
            arrow.Points.Add(new Point(vertexPoint.X + size, vertexPoint.Y - size));
            return arrow;
        }

        /// <summary>
        /// Generates an horizontal arrow polyline
        /// </summary>
        /// <param name="vertexPoint">The vertex point of the arrow</param>
        /// <param name="size">The arrow size</param>
        /// <returns>The arrow</returns>
        private static Polyline HorizontalArrow(Point vertexPoint, double size)
        {
            Polyline arrow = new Polyline();
            arrow.Points.Add(new Point(vertexPoint.X - size, vertexPoint.Y + size));
            arrow.Points.Add(vertexPoint);
            arrow.Points.Add(new Point(vertexPoint.X - size, vertexPoint.Y - size));
            return arrow;
        }

        /// <summary>
        /// Creates a Tooltip for a polyline point
        /// </summary>
        /// <param name="text">The text shown on the tooltip</param>
        /// <returns>The tooltip</returns>
        private static ToolTip PointToolTip(string text)
        {
            TextBlock t = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White
            };

            Border tooltipContent = new Border
            {
                CornerRadius = new CornerRadius(7),
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(5),
                Background = new SolidColorBrush(Color.FromArgb(192, 0, 0, 0)),
                Child = t
            };

            ToolTip tip = new ToolTip
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                BorderBrush = Brushes.Transparent,
                Content = tooltipContent
            };

            return tip;
        }
        #endregion

        #region Public Propeties
        /// <summary>
        /// The values of each polyline
        /// </summary>
        public ObservableRangeCollection<KeyValuePair<int[],SolidColorBrush>> LinesValues
        {
            get { return (ObservableRangeCollection<KeyValuePair<int[], SolidColorBrush>>)GetValue(LinesValuesProperty); }
            set { SetValue(LinesValuesProperty, value); }
        }
        /// <summary>
        /// The labels undet the horizontal axis line
        /// </summary>
        public ObservableRangeCollection<string> HorizontalLables
        {
            get { return (ObservableRangeCollection<string>)GetValue(HorizontalLablesProperty); }
            set { SetValue(HorizontalLablesProperty, value); }
        }
        
        /// <summary>
        /// The number of steps in the horizontal axis
        /// </summary>
        public int HorizontalSteps => HorizontalLables.Count;

        /// <summary>
        /// The color of the labels
        /// </summary>
        public SolidColorBrush LabelsColor
        {
            get { return (SolidColorBrush)GetValue(LabelsColorProperty); }
            set { SetValue(LabelsColorProperty, value); }
        }

        /// <summary>
        /// The graph distance from the axis
        /// </summary>
        public Double GraphContentPadding
        {
            get { return (Double)GetValue(GraphContentPaddingProperty); }
            set { SetValue(GraphContentPaddingProperty, value); }
        }

        /// <summary>
        /// The thickness of the axis
        /// </summary>
        public Double AxisThickness
        {
            get { return (Double)GetValue(AxisThicknessProperty); }
            set { SetValue(AxisThicknessProperty, value); }
        }

        /// <summary>
        /// The number of steps on the Y axis (could be less)
        /// </summary>
        public Double VerticalSteps
        {
            get { return (Double)GetValue(VerticalStepsProperty); }
            set { if (value > 0) SetValue(VerticalStepsProperty, value); }
        }

        /// <summary>
        /// The color of the axis
        /// </summary>
        public SolidColorBrush AxisColor
        {
            get { return (SolidColorBrush)GetValue(AxisColorProperty); }
            set { SetValue(AxisColorProperty, value); }
        }

        /// <summary>
        /// The size of the points
        /// </summary>
        public double PointSize
        {
            get { return (double)GetValue(PointSizeProperty); }
            set { SetValue(PointSizeProperty, value); }
        }

        /// <summary>
        /// The lenght of the axis line out of the graph space
        /// </summary>
        public double AxisOutlineLenght
        {
            get { return (double)GetValue(AxisOutlineLenghtProperty); }
            set { SetValue(AxisOutlineLenghtProperty, value); }
        }

        /// <summary>
        /// The Y axis name
        /// </summary>
        public string YAxisName
        {
            get { return (string)GetValue(YAxisNameProperty); }
            set { SetValue(YAxisNameProperty, value); }
        }

        /// <summary>
        /// The X axis name
        /// </summary>
        public string XAxisName
        {
            get { return (string)GetValue(XAxisNameProperty); }
            set { SetValue(XAxisNameProperty, value); }
        }

        /// <summary>
        /// The space between the horizontal axis steps
        /// </summary>
        public int HorizontalStepsWidth
        {
            get { return (int)GetValue(HorizontalStepsWidthProperty); }
            set { SetValue(HorizontalStepsWidthProperty, value); }
        }

        /// <summary>
        /// The height of the graph
        /// </summary>
        public double GraphHeight
        {
            get { return (double)GetValue(GraphHeightProperty); }
            set { SetValue(GraphHeightProperty, value); }
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty LinesValuesProperty =
            DependencyProperty.Register("LinesValues", typeof(ObservableRangeCollection<KeyValuePair<int[], SolidColorBrush>>), typeof(MultiPointsGraph), new FrameworkPropertyMetadata(null, OnValuesChanged));
        public static readonly DependencyProperty HorizontalLablesProperty =
            DependencyProperty.Register("HorizontalLables", typeof(ObservableRangeCollection<string>), typeof(MultiPointsGraph), new FrameworkPropertyMetadata(null, OnLabelsChanged));
        public static readonly DependencyProperty LabelsColorProperty =
            DependencyProperty.Register("LabelsColor", typeof(SolidColorBrush), typeof(MultiPointsGraph), new PropertyMetadata(Brushes.Black));
        public static readonly DependencyProperty GraphContentPaddingProperty =
            DependencyProperty.Register("GraphContentPadding", typeof(Double), typeof(MultiPointsGraph), new PropertyMetadata((Double)20));
        public static readonly DependencyProperty AxisThicknessProperty =
            DependencyProperty.Register("AxisThickness", typeof(Double), typeof(MultiPointsGraph), new PropertyMetadata((Double)10));
        public static readonly DependencyProperty VerticalStepsProperty =
            DependencyProperty.Register("VerticalSteps", typeof(Double), typeof(MultiPointsGraph), new PropertyMetadata((Double)5));
        public static readonly DependencyProperty AxisColorProperty =
            DependencyProperty.Register("AxisColor", typeof(SolidColorBrush), typeof(MultiPointsGraph), new PropertyMetadata(Brushes.Black));
        public static readonly DependencyProperty PointSizeProperty =
            DependencyProperty.Register("PointSize", typeof(double), typeof(MultiPointsGraph), new PropertyMetadata((Double)5));
        public static readonly DependencyProperty AxisOutlineLenghtProperty =
            DependencyProperty.Register("AxisOutlineLenght", typeof(double), typeof(MultiPointsGraph), new PropertyMetadata((Double)10));
        public static readonly DependencyProperty YAxisNameProperty =
            DependencyProperty.Register("YAxisName", typeof(string), typeof(MultiPointsGraph), new PropertyMetadata(""));
        public static readonly DependencyProperty XAxisNameProperty =
            DependencyProperty.Register("XAxisName", typeof(string), typeof(MultiPointsGraph), new PropertyMetadata(""));        
        public static readonly DependencyProperty HorizontalStepsWidthProperty =
            DependencyProperty.Register("HorizontalStepsWidth", typeof(int), typeof(MultiPointsGraph), new PropertyMetadata(80));
        public static readonly DependencyProperty GraphHeightProperty =
            DependencyProperty.Register("GraphHeight", typeof(double), typeof(MultiPointsGraph), new PropertyMetadata((double)300));
        #endregion

        #region Private Methods
        private static void OnValuesChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MultiPointsGraph graph = sender as MultiPointsGraph;
            if (e.OldValue is ObservableRangeCollection<KeyValuePair<int[], SolidColorBrush>> old_ && old_ != null)
                old_.CollectionChanged -= graph.UpdateGraph;
            if (e.NewValue is ObservableRangeCollection<KeyValuePair<int[], SolidColorBrush>> new_ && new_ != null)
                new_.CollectionChanged += graph.UpdateGraph;
        }
        private static void OnLabelsChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MultiPointsGraph graph = sender as MultiPointsGraph;

            if (e.OldValue is ObservableRangeCollection<string> old_ && old_ != null)
                old_.CollectionChanged -= graph.SetupXaxis;
            if (e.NewValue is ObservableRangeCollection<string> new_ && new_ != null)
                new_.CollectionChanged += graph.SetupXaxis;
        }

        public void Refresh(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Check() is false) return;
            FindYAxisBounds();
            SetupYAxis();
            SetupXaxis(null,null);
            UpdateGraph(null,null);
        }
        private bool Check()
        {
            if (LinesValues is null) return false;
            if (HorizontalLables is null) return false;
            if (HorizontalLables.Count == 0) return false;
            int n = HorizontalSteps;
            foreach (var line in LinesValues)
            {
                if (line.Key.Length != n) return false;
            }
            return true;
        }

        private void UpdateGraph(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Check() is false) return;
            FindYAxisBounds();
            SetupYAxis();
            Graph.Children.Clear();
            List<Ellipse> points = new List<Ellipse>();
            lines = new Polyline[LinesValues.Count];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = new Polyline
                {
                    Stroke = LinesValues[i].Value
                };
            }

            //Setting the horizontal offset for the axis units and lines points
            Double offset = AxisThickness + GraphContentPadding;
            //for each horizontal unit:
            for (int i = 0; i < HorizontalSteps; i++)
            {
                //foreach line
                for (int j = 0; j < LinesValues.Count; j++)
                {
                    //adding the point to the polyline in the corrisponding horizontal unit
                    lines[j].Points.Add(new Point(offset, ((double)((double)((double)(LinesValues[j].Key[i] - minY)) / (maxY - minY))) * (GraphHeight) + AxisThickness + GraphContentPadding));
                    //adding the ellipse and tooltip to the point
                    Ellipse ec = new Ellipse
                    {
                        Width = PointSize,
                        Height = PointSize,
                        Fill = lines[j].Stroke,
                        ToolTip = PointToolTip(LinesValues[j].Key[i].ToString())
                    };
                    Canvas.SetLeft(ec, offset - PointSize / 2);
                    Canvas.SetTop(ec, ((double)((double)((double)(LinesValues[j].Key[i] - minY)) / (maxY - minY))) * (GraphHeight) + AxisThickness + GraphContentPadding - PointSize / 2);
                    points.Add(ec);
                }
                offset += HorizontalStepsWidth;
            }
            //Adding the polylines to the graph
            foreach (var line in lines)
                Graph.Children.Add(line);
            foreach (var ec in points)
                Graph.Children.Add(ec);
            
        }

        private void SetupXaxis(object sender, NotifyCollectionChangedEventArgs e)
        {
            XAxis.Children.Clear();
            XLabels.Children.Clear();

            double hlineW = HorizontalStepsWidth * (HorizontalSteps - 1) + AxisThickness + GraphContentPadding + AxisOutlineLenght;

            //Adding the horizontal axis line
            xAxisLine = new Polyline();
            xAxisLine.Points.Add(new Point(AxisThickness / 2, AxisThickness));
            xAxisLine.Points.Add(new Point(hlineW, AxisThickness));
            xAxisLine.Stroke = AxisColor;
            xAxisLine.StrokeThickness = 0.5;

            //Setting the horizontal offset for the axis units and lines points
            Double offset = AxisThickness + GraphContentPadding;
            //for each horizontal unit:
            for (int i = 0; i < HorizontalSteps; i++)
            {
                //Adding the horizontal axis units lines
                Polyline line = new Polyline();
                line.Points.Add(new Point(offset, AxisThickness / 2));
                line.Points.Add(new Point(offset, AxisThickness));
                line.Stroke = AxisColor;
                line.Visibility = Visibility.Visible;
                line.StrokeThickness = 0.5;
                XAxis.Children.Add(line);

                //Adding the horizontal axis units labels
                TextBlock stepLabel = new TextBlock
                {
                    Text = HorizontalLables[i],
                    FontSize = 10,
                    Foreground = LabelsColor
                };
                Canvas.SetBottom(stepLabel, -2 * stepLabel.FontSize);
                Canvas.SetLeft(stepLabel, offset);
                XLabels.Children.Add(stepLabel);
                offset += HorizontalStepsWidth;
            }


            //Adding the X axis name label
            Double XPosition = hlineW + 5;
            Double YPosition = AxisThickness / 2;
            Decorator label = CanvasLabel(XAxisName, XPosition, YPosition, LabelsColor, 10);
            label.Width = 50;
            label.Height = 15;
            XLabels.Children.Add(label);

            //Creating the arrow for the end of the axis line
            Polyline arrow = HorizontalArrow(new Point(hlineW, AxisThickness), 5);
            arrow.Stroke = AxisColor;
            arrow.StrokeThickness = 1;

            //Adding the X axis line and his arrow
            XAxis.Children.Add(xAxisLine);
            XAxis.Children.Add(arrow);
            Graph.Width = hlineW + label.Width + 10;
        }


        private void SetupYAxis()
        {
            YAxis.Children.Clear();
            YLabels.Children.Clear();

            //Adding the vertical axis line
            yAxisLine = new Polyline();
            double lineH = GraphHeight + AxisThickness + GraphContentPadding + AxisOutlineLenght;
            yAxisLine.Points.Add(new Point(AxisThickness, AxisThickness / 2));
            yAxisLine.Points.Add(new Point(AxisThickness, lineH));
            yAxisLine.Stroke = AxisColor;
            yAxisLine.StrokeThickness = 0.5;

            //Adding the labels and the unit lines

            //Calculatin the steps height
            Double offset = 0;
            int n_division = 0;
            double stepHeight = GraphHeight;
            while (stepHeight > 10)
            {
                n_division++;
                stepHeight = stepHeight / 10;
            }
            //if ((stepHeight - Math.Round(stepHeight)) < 0.5) stepHeight += 0.5;
            stepHeight = Math.Round(stepHeight, MidpointRounding.AwayFromZero);
            for (int i = 0; i < n_division; i++)
                stepHeight = stepHeight * 10;
            stepHeight = stepHeight / (VerticalSteps - 1);

            //Adding the lines
            for (int i = 0; i < VerticalSteps; i++)
            {
                double verticalPosition = AxisThickness + GraphContentPadding + offset;
                Polyline line = new Polyline();
                line.Points.Add(new Point(AxisThickness / 2, verticalPosition));
                line.Points.Add(new Point(AxisThickness, verticalPosition));
                line.Stroke = AxisColor;
                line.StrokeThickness = 0.5;
                YAxis.Children.Add(line);

                //Adding the labels

                TextBlock stepLabel = new TextBlock
                {
                    Text = Math.Round((i * stepHeight) * (maxY - minY) / GraphHeight + minY).ToString(),
                    FontSize = 8,
                    Foreground = LabelsColor
                };
                Canvas.SetBottom(stepLabel, verticalPosition - stepLabel.FontSize / 2);
                Canvas.SetLeft(stepLabel, -((stepLabel.Text.Length * stepLabel.FontSize) / 2));
                YLabels.Children.Add(stepLabel);

                offset += stepHeight;
                //Avoiding lines over the vertical axis arrow
                if ((verticalPosition + stepHeight) > lineH - AxisOutlineLenght)
                    break;
            }

            //Adding the Y axis name label
            Decorator label = CanvasLabel(YAxisName, -AxisThickness, lineH + 5, LabelsColor, 10);
            label.Width = 50;
            label.Height = 15;
            YLabels.Children.Add(label);

            //Adding the arrow on top of the axis line
            Polyline arrow = VerticalArrow(new Point(AxisThickness, lineH), AxisThickness / 2);
            arrow.Stroke = AxisColor;
            arrow.StrokeThickness = 1;
            YAxis.Children.Add(yAxisLine);
            YAxis.Children.Add(arrow);
            Graph.Height = lineH + label.Height + 10;
        }

        private void FindYAxisBounds()
        {
            minY = int.MaxValue;
            maxY = int.MinValue;
            if (LinesValues != null && LinesValues.Count > 0)
            {
                foreach (var line_values in LinesValues)
                {
                    foreach (int v in line_values.Key)
                    {
                        if (v < (minY + 20)) minY = v - 20;
                        if (v > maxY) maxY = v;
                    }
                }
            }
            if (maxY < 10) maxY = 10;
            if (minY < 0 || minY > maxY) minY = 0;

        }
        #endregion
    }
}
