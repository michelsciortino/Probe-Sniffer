using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProbeSniffer.Controls
{
    /// <summary>
    /// Logica di interazione per PolylineGraph.xaml
    /// </summary>
    public partial class PolylineGraph : UserControl
    {
        #region Private
        private Double max_x, max_y, min_x, min_y;
        private Double stepHeight;
        #endregion

        public PolylineGraph()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The list of point shown on the graph
        /// </summary>
        public ICollection<Point> ItemsSource
        {
            get { return (ICollection<Point>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ICollection<Point>), typeof(PolylineGraph),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnItemsSourceChanged)));

        /// <summary>
        /// Redraws the view when the Points collection changes
        /// </summary>
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PolylineGraph graph = (PolylineGraph)d;

            if (e.NewValue == null) return;

            graph.Graph.Children.Clear();
            graph.Axis.Children.Clear();

            graph.max_x = Double.MinValue;
            graph.max_y = Double.MinValue;
            graph.min_x = Double.MaxValue;
            graph.min_y = Double.MaxValue;

            ICollection<Point> scaledPoints = new Collection<Point>();
            foreach(Point p in (ICollection<Point>)e.NewValue)
                scaledPoints.Add(new Point(p.X * graph.XScaleFactor, p.Y *graph.YScaleFactor));

            
            foreach (Point p in scaledPoints)
            {
                if (p.X > graph.max_x) graph.max_x = p.X;
                if (p.Y > graph.max_y) graph.max_y = p.Y;
                if (p.X < graph.min_x) graph.min_x = p.X;
                if (p.Y < graph.min_y) graph.min_y = p.Y;
            }
            Double height = (graph.max_y - graph.min_y);
            Double width = (graph.max_x - graph.min_x);

            graph.Graph.Width = width + graph.AxisThickness +((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0) + graph.AxisOutlineLenght;
            graph.Graph.Height = height + graph.AxisThickness + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0) + graph.AxisOutlineLenght;

            SetUpYAxis(graph, scaledPoints);
            SetUpXAxis(graph, scaledPoints);
            SetUpGraph(graph, scaledPoints);
        }

        #region Graph Setup
        /// <summary>
        /// Sets up the X axis
        /// </summary>
        /// <param name="graph">The Graph</param>
        /// <param name="newPoints">The list of Points on the graph</param>
        private static void SetUpXAxis(PolylineGraph graph, ICollection<Point> newPoints)
        {
            //Adding the horizontal axis line
            Polyline horizontalLine = new Polyline();
            horizontalLine.Points.Add(new Point(graph.AxisThickness / 2, graph.AxisThickness));
            horizontalLine.Points.Add(new Point(graph.max_x + graph.AxisThickness -graph.min_x + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0) + graph.AxisOutlineLenght, graph.AxisThickness));
            horizontalLine.Stroke = graph.AxisColor;
            horizontalLine.StrokeThickness = 0.5;
            graph.Axis.Children.Add(horizontalLine);

            //Setting the horizontal offset for the axis units and lines
            Double offset = graph.AxisThickness;
            if (graph.AttachGraphToAxis is false)
                offset += graph.GraphContentPadding;

            //Adding the axis unit lines and labels
            foreach(Point p in newPoints)
            {
                if (graph.AttachGraphToAxis && p.X == 0) continue;

                //Adding the lines
                Polyline line = new Polyline();
                line.Points.Add(new Point(offset+p.X - graph.min_x, graph.AxisThickness/2));
                line.Points.Add(new Point(offset + p.X - graph.min_x, graph.AxisThickness));
                line.Stroke = graph.AxisColor;
                line.StrokeThickness = 0.5;
                graph.Axis.Children.Add(line);

                //Adding the labels
                if (graph.ShowAxisLabels)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = ((p.X) / graph.XScaleFactor).ToString(),
                        FontSize = 8,
                        Foreground = graph.LabelsColor
                    };
                    Canvas.SetBottom(label, -label.FontSize);
                    Canvas.SetLeft(label, offset + p.X -graph.min_x - ((label.Text.Length*label.FontSize)/4));
                    graph.Labels.Children.Add(label);
                }
            }

            //Adding the X axis name label
            if (graph.ShowAxisLabels)
            {
                Double XPosition = graph.AxisThickness + (graph.max_x - graph.min_x) + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0) + graph.AxisOutlineLenght + 5;
                Double YPosition = graph.AxisThickness/2;
                Decorator label = Label(graph.XAxisName, XPosition, YPosition, graph.LabelsColor, 10);
                label.Width = 50;
                label.Height = 15;
                graph.Labels.Children.Add(label);
                graph.Graph.Width += label.Width + 8;
            }

            //Adding the arrow to the end of the axis line
            Polyline arrow = HorizontalArrow(new Point(graph.max_x - graph.min_x + graph.AxisThickness + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0) + graph.AxisOutlineLenght, graph.AxisThickness), graph.AxisThickness / 2);
            arrow.Stroke = graph.AxisColor;
            arrow.StrokeThickness = 1;
            graph.Axis.Children.Add(arrow);
        }

        /// <summary>
        /// Sets up the Y axis
        /// </summary>
        /// <param name="graph">The Graph</param>
        /// <param name="newPoints">The list of points on the graph</param>
        private static void SetUpYAxis(PolylineGraph graph, ICollection<Point> newPoints)
        {
            //Adding the vertical axis line
            Polyline verticalLine = new Polyline();
            double lineH = graph.max_y - graph.min_y + graph.AxisThickness + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0);
            verticalLine.Points.Add(new Point(graph.AxisThickness, graph.AxisThickness / 2));
            verticalLine.Points.Add(new Point(graph.AxisThickness, lineH + graph.AxisOutlineLenght));
            verticalLine.Stroke = graph.AxisColor;
            verticalLine.StrokeThickness = 0.5;
            graph.Axis.Children.Add(verticalLine);
            
            //Adding the labels and the unit lines
            if (graph.VerticalSteps > 0)
            {
                //Calculatin the steps height
                Double offset = 0;
                int n_division = 0;
                if (graph.VerticalSteps <= 0) graph.VerticalSteps = 1;
                graph.stepHeight = (graph.max_y - graph.min_y);
                while (graph.stepHeight > 10)
                {
                    n_division++;
                    graph.stepHeight = graph.stepHeight / 10;
                }
                if ((graph.stepHeight - Math.Round(graph.stepHeight)) < 0.5) graph.stepHeight += 0.5;
                graph.stepHeight = Math.Round(graph.stepHeight, MidpointRounding.AwayFromZero);
                for (int i = 0; i < n_division; i++)
                    graph.stepHeight = graph.stepHeight * 10;
                graph.stepHeight = graph.stepHeight / (graph.VerticalSteps - 1);

                
                //Adding the lines
                for (int i = 0; i < graph.VerticalSteps;i++)
                {
                    if (graph.AttachGraphToAxis && offset== 0)
                    {
                        i--;
                        offset += graph.stepHeight;
                        continue;
                    }
                    Polyline line = new Polyline();
                    line.Points.Add(new Point(graph.AxisThickness / 2, graph.AxisThickness + offset + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0)));
                    line.Points.Add(new Point(graph.AxisThickness, graph.AxisThickness + offset + ((!graph.AttachGraphToAxis)?graph.GraphContentPadding:0)));
                    line.Stroke = graph.AxisColor;
                    line.StrokeThickness = 0.5;
                    graph.Axis.Children.Add(line);

                    //Adding the labels
                    if (graph.ShowAxisLabels)
                    {
                        TextBlock label = new TextBlock();
                        label.Text = Math.Round(((offset + graph.min_y)/graph.YScaleFactor)).ToString();
                        label.FontSize = 8;
                        label.Foreground = graph.LabelsColor;
                        Canvas.SetBottom(label, graph.AxisThickness + offset + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0) - label.FontSize/2);
                        Canvas.SetLeft(label, - ((label.Text.Length * label.FontSize)/2));
                        graph.Labels.Children.Add(label);
                    }
                    offset += graph.stepHeight;
                    //Avoiding lines over the vertical axis arrow
                    if (graph.AxisThickness + offset + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0) > lineH)
                        break;
                }
            }

            //Adding the Y axis name label
            if (graph.ShowAxisLabels)
            {                
                Double YPosition = graph.AxisThickness + (graph.max_y - graph.min_y) + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0) + graph.AxisOutlineLenght + 5;
                Double XPosition = -graph.AxisThickness;
                Decorator label = Label(graph.YAxisName, XPosition, YPosition, graph.LabelsColor, 10);
                label.Width = 50;
                label.Height = 15;
                graph.Labels.Children.Add(label);
                graph.Graph.Height += label.Height + 8;                
            }
            //Adding the arrow on top of the axis line
            Polyline arrow = VerticalArrow(new Point(graph.AxisThickness, graph.max_y - graph.min_y + graph.AxisThickness + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0) + graph.AxisOutlineLenght),graph.AxisThickness/2);
            arrow.Stroke = graph.AxisColor;
            arrow.StrokeThickness = 1;
            graph.Axis.Children.Add(arrow);
        }

        /// <summary>
        /// Sets up the polyline and points on the graph
        /// </summary>
        /// <param name="graph">The Graph</param>
        /// <param name="newPoints">The list of points of the polyline on the graph</param>
        private static void SetUpGraph(PolylineGraph graph, ICollection<Point> newPoints)
        {
            //Adding the Polyline
            Polyline polyline = new Polyline
            {
                Points = new PointCollection(newPoints),
                Stroke = graph.PolylineColor,
                StrokeThickness = 1
            };
            Canvas.SetLeft(polyline, graph.AxisThickness -graph.min_x + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0));
            Canvas.SetTop(polyline, graph.AxisThickness -graph.min_y + + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0));
            graph.Graph.Children.Add(polyline);

            //Adding the points with tooltip
            if (graph.ShowPoints)
            {
                foreach(Point p in newPoints)
                {
                    Ellipse e = new Ellipse
                    {
                        Width = graph.PointSize,
                        Height = graph.PointSize,
                        Fill = graph.PolylineColor,
                        ToolTip = PointToolTip((p.Y / graph.YScaleFactor).ToString())
                    };
                    Canvas.SetLeft(e, graph.AxisThickness + p.X -graph.min_x - graph.PointSize / 2 + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0));
                    Canvas.SetTop(e, graph.AxisThickness + p.Y - graph.min_y - graph.PointSize / 2 + ((!graph.AttachGraphToAxis) ? graph.GraphContentPadding : 0));
                    graph.Graph.Children.Add(e);
                }
            }
        }

        /// <summary>
        /// Label generator for Canvas containers
        /// </summary>
        /// <param name="text">The text of the label</param>
        /// <param name="x">The left margin from the canvas border</param>
        /// <param name="y">The bottom margin from the canvas border</param>
        /// <param name="foreground">The foreground of the label</param>
        /// <param name="fontSize">The font size of the label</param>
        /// <returns></returns>
        private static Decorator Label(string text,Double x, Double y,Brush foreground,double fontSize)
        {
            TextBlock label = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = foreground
            };
            Decorator container = new Viewbox();
            container.Child = label;

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
        private static Polyline VerticalArrow(Point vertexPoint,double size)
        {
            Polyline arrow = new Polyline();
            arrow.Points.Add(new Point(vertexPoint.X-size,vertexPoint.Y-size));
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
        /// Creates a Tooltip for UIElements
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
                Background= new SolidColorBrush(Color.FromArgb(192, 0, 0, 0)),
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

        #region Dependency Properties
        /// <summary>
        /// Enables Points visualization on the graph
        /// </summary>
        public bool ShowPoints
        {
            get { return (bool)GetValue(ShowPointsProperty); }
            set { SetValue(ShowPointsProperty, value); }
        }
        public static readonly DependencyProperty ShowPointsProperty =
            DependencyProperty.Register("ShowPoints", typeof(bool), typeof(PolylineGraph), new PropertyMetadata(true));

        /// <summary>
        /// Enables the axis labels
        /// </summary>
        public bool ShowAxisLabels
        {
            get { return (bool)GetValue(ShowAxisLabelsProperty); }
            set { SetValue(ShowAxisLabelsProperty, value); }
        }
        public static readonly DependencyProperty ShowAxisLabelsProperty =
            DependencyProperty.Register("ShowAxisLabels", typeof(bool), typeof(PolylineGraph), new PropertyMetadata(true));

        /// <summary>
        /// The color of the labels
        /// </summary>
        public Brush LabelsColor
        {
            get { return (Brush)GetValue(LabelsColorProperty); }
            set { SetValue(LabelsColorProperty, value); }
        }
        public static readonly DependencyProperty LabelsColorProperty =
            DependencyProperty.Register("LabelsColor", typeof(Brush), typeof(PolylineGraph), new PropertyMetadata(Brushes.Black));
        /// <summary>
        /// Aligns the graph to the left bottom corner
        /// </summary>
        public bool AttachGraphToAxis
        {
            get { return (bool)GetValue(AttachGraphToAxisProperty); }
            set { SetValue(AttachGraphToAxisProperty, value); }
        }
        public static readonly DependencyProperty AttachGraphToAxisProperty =
            DependencyProperty.Register("AttachGraphToAxis", typeof(bool), typeof(PolylineGraph), new PropertyMetadata(false));

        /// <summary>
        /// The graph distance from the axis
        /// </summary>
        public Double GraphContentPadding
        {
            get { return (Double)GetValue(GraphContentPaddingProperty); }
            set { SetValue(GraphContentPaddingProperty, value); }
        }
        public static readonly DependencyProperty GraphContentPaddingProperty =
            DependencyProperty.Register("GraphContentPadding", typeof(Double), typeof(PolylineGraph), new PropertyMetadata((Double)0));

        /// <summary>
        /// The thickness of the axis
        /// </summary>
        public Double AxisThickness
        {
            get { return (Double)GetValue(AxisThicknessProperty); }
            set { SetValue(AxisThicknessProperty, value); }
        }
        public static readonly DependencyProperty AxisThicknessProperty =
            DependencyProperty.Register("AxisThickness", typeof(Double), typeof(PolylineGraph), new PropertyMetadata((Double)10));

        /// <summary>
        /// The number of steps on the Y axis (could be less)
        /// </summary>
        public Double VerticalSteps
        {
            get { return (Double)GetValue(VerticalStepsProperty); }
            set { SetValue(VerticalStepsProperty, value); }
        }
        public static readonly DependencyProperty VerticalStepsProperty =
            DependencyProperty.Register("VerticalSteps", typeof(Double), typeof(PolylineGraph), new PropertyMetadata((Double)0));
        
        /// <summary>
        /// The color of the axis
        /// </summary>
        public Brush AxisColor
        {
            get { return (Brush)GetValue(AxisColorProperty); }
            set { SetValue(AxisColorProperty, value); }
        }
        public static readonly DependencyProperty AxisColorProperty =
            DependencyProperty.Register("AxisColor", typeof(Brush), typeof(PolylineGraph), new PropertyMetadata(Brushes.Black));

        /// <summary>
        /// The color of the polyline
        /// </summary>
        public Brush PolylineColor
        {
            get { return (Brush)GetValue(PolylineColorProperty); }
            set { SetValue(PolylineColorProperty, value); }
        }
        public static readonly DependencyProperty PolylineColorProperty =
            DependencyProperty.Register("PolylineColor", typeof(Brush), typeof(PolylineGraph), new PropertyMetadata(Brushes.Black));

        /// <summary>
        /// The size of the points
        /// </summary>
        public double PointSize
        {
            get { return (double)GetValue(PointSizeProperty); }
            set { SetValue(PointSizeProperty, value); }
        }
        public static readonly DependencyProperty PointSizeProperty =
            DependencyProperty.Register("PointSize", typeof(double), typeof(PolylineGraph), new PropertyMetadata((Double)5));

        /// <summary>
        /// The lenght of the axis line out of the graph space
        /// </summary>
        public double AxisOutlineLenght
        {
            get { return (double)GetValue(AxisOutlineLenghtProperty); }
            set { SetValue(AxisOutlineLenghtProperty, value); }
        }
        public static readonly DependencyProperty AxisOutlineLenghtProperty =
            DependencyProperty.Register("AxisOutlineLenght", typeof(double), typeof(PolylineGraph), new PropertyMetadata((Double)10));

        /// <summary>
        /// The scale factor of the X axis
        /// </summary>
        public Double XScaleFactor
        {
            get { return (Double)GetValue(XScaleFactorProperty); }
            set { SetValue(XScaleFactorProperty, value); }
        }
        public static readonly DependencyProperty XScaleFactorProperty =
            DependencyProperty.Register("XScaleFactor", typeof(Double), typeof(PolylineGraph), new PropertyMetadata((Double)1));

        /// <summary>
        /// The scale factor of the Y axis
        /// </summary>
        public Double YScaleFactor
        {
            get { return (Double)GetValue(YScaleFactorProperty); }
            set { SetValue(YScaleFactorProperty, value); }
        }
        public static readonly DependencyProperty YScaleFactorProperty =
            DependencyProperty.Register("YScaleFactor", typeof(Double), typeof(PolylineGraph), new PropertyMetadata((Double)1));

        /// <summary>
        /// The Y axis name
        /// </summary>
        public string YAxisName
        {
            get { return (string)GetValue(YAxisNameProperty); }
            set { SetValue(YAxisNameProperty, value); }
        }
        public static readonly DependencyProperty YAxisNameProperty =
            DependencyProperty.Register("YAxisName", typeof(string), typeof(PolylineGraph), new PropertyMetadata(""));

        /// <summary>
        /// The X axis name
        /// </summary>
        public string XAxisName
        {
            get { return (string)GetValue(XAxisNameProperty); }
            set { SetValue(XAxisNameProperty, value); }
        }
        public static readonly DependencyProperty XAxisNameProperty =
            DependencyProperty.Register("XAxisName", typeof(string), typeof(PolylineGraph), new PropertyMetadata(""));

        #endregion
    }
}
