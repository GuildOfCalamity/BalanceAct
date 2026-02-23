using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;

namespace BalanceAct.Models
{
    public class ChartPoint
    {
        public DateTime Time { get; set; }
        public double Value { get; set; }
        public string Uom { get; set; } // unit of measure
        public string Info { get; set; } // description or details
        public ChartPoint(DateTime time, double value, string uom, string info)
        {
            Time = time;
            Value = value;
            Uom = uom;
            Info = info;
        }
    }

    public class ChartSeries
    {
        public List<ChartPoint> Points { get; set; } = new();
        public Brush Stroke { get; set; } = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 70, 160));
        public Brush Fill { get; set; } = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 10, 120));
        public double StrokeThickness { get; set; } = 3.5;

        #region [Gridlines]
        public Brush GridPen { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 255, 255, 255));
        public double GridThickness { get; set; } = 1.2;
        #endregion

        #region [Static points]
        public bool ShowPoints { get; set; } = true;
        #endregion
    }
}
