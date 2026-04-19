using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace RyTuneX.Controls;

// A scrolling usage graph that displays a rolling history of percentage values as a filled line chart.
public sealed partial class UsageGraph : UserControl
{
    private const int MaxDataPoints = 60;
    private readonly List<double> _values = new(MaxDataPoints);

    // The color used for the graph line and fill gradient. Defaults to the system accent color.
    public static readonly DependencyProperty GraphColorProperty =
        DependencyProperty.Register(
            nameof(GraphColor),
            typeof(Color),
            typeof(UsageGraph),
            new PropertyMetadata(default(Color), OnGraphColorChanged));

    public Color GraphColor
    {
        get => (Color)GetValue(GraphColorProperty);
        set => SetValue(GraphColorProperty, value);
    }

    private static void OnGraphColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((UsageGraph)d).ApplyGraphColor();
    }

    public UsageGraph()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyGraphColor();
    }

    private void ApplyGraphColor()
    {
        var color = GraphColor;

        // If no color was set (default), fall back to theme accent
        if (color == default)
        {
            if (Resources.TryGetValue("SystemAccentColor", out var res) ||
                Application.Current.Resources.TryGetValue("SystemAccentColor", out res))
            {
                color = (Color)res;
            }
            else
            {
                color = Color.FromArgb(255, 0, 120, 215);
            }
        }

        FillPolygon.Fill = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(0, 1),
            GradientStops =
            {
                new GradientStop { Color = color, Offset = 0 },
                new GradientStop { Color = Color.FromArgb(0, 0, 0, 0), Offset = 1 }
            }
        };

        LinePolyline.Stroke = new SolidColorBrush(color);
    }

    // Add a new percentage value (0-100) to the graph and redraws.
    public void AddValue(double percent)
    {
        percent = Math.Clamp(percent, 0, 100);

        _values.Add(percent);
        if (_values.Count > MaxDataPoints)
        {
            _values.RemoveAt(0);
        }

        Redraw();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Redraw();
    }

    private void Redraw()
    {
        var width = ActualWidth;
        var height = ActualHeight;

        if (width <= 0 || height <= 0 || _values.Count == 0)
        {
            return;
        }

        var count = _values.Count;
        var stepX = width / (MaxDataPoints - 1);

        // Offset so the newest point is always at the right edge
        var startX = width - (count - 1) * stepX;

        var linePoints = new PointCollection();
        var fillPoints = new PointCollection();

        // Bottom-left corner of fill area
        fillPoints.Add(new Windows.Foundation.Point(startX, height));

        for (var i = 0; i < count; i++)
        {
            var x = startX + i * stepX;
            var y = height - (_values[i] / 100.0 * height);
            var point = new Windows.Foundation.Point(x, y);

            linePoints.Add(point);
            fillPoints.Add(point);
        }

        // Bottom-right corner of fill area
        fillPoints.Add(new Windows.Foundation.Point(startX + (count - 1) * stepX, height));

        LinePolyline.Points = linePoints;
        FillPolygon.Points = fillPoints;
    }
}
