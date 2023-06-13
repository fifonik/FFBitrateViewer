using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Legends;
using System.Collections.Generic;

namespace FFBitrateViewer
{
    public class SerieStyle
    {
        public OxyColor     Color       = OxyColors.Black;
        public LineStyle    LineStyle   = LineStyle.Solid;
        public MarkerType   MarkerType  = MarkerType.None;

        public SerieStyle() { }


        public SerieStyle(OxyColor color, LineStyle? style = null)
        {
            Color = color;
            if(style != null) LineStyle = (LineStyle)style;
        }


        public SerieStyle(string color, string? style = null)
        {
            Color = OxyColor.Parse(color);
            if(style != null) LineStyleSet(style);
        }


        public string ColorToString()
        {
            return Color.ToString();
        }


        public void ColorSet(string color)
        {
            Color = OxyColor.Parse(color);
        }


        public string LineStyleToString()
        {
            return LineStyle.ToString();
        }


        public void LineStyleSet(string style)
        {
            switch (style.ToUpper())
            {
                case "DASH":
                    LineStyle = LineStyle.Dash;
                    break;
                case "DOT":
                    LineStyle = LineStyle.Dot;
                    break;
                case "SOLID":
                    LineStyle = LineStyle.Solid;
                    break;
                default:
                    LineStyle = LineStyle.Solid; // todo@ exception
                    break;
            }
        }
    }


    public class SeriesParams
    {
        public SerieStyle SerieStyle = new();
        public string     Color {
            get { return SerieStyle.ColorToString(); }
            set { SerieStyle.ColorSet(value); }
        }
        public string     Style {
            get { return SerieStyle.LineStyleToString(); }
            set { SerieStyle.LineStyleSet(value); }
        }
    }


    public class PlotParams
    {
        public List<SeriesParams> Series { get; set; } = new List<SeriesParams>();
    }


    public class MyPlotModel : PlotModel
    {
        /* Named colors to consider:
            000000 Black
            0000FF Blue
            8A2BE2 BlueViolet
            A52A2A Brown
            7FFF00 Charteuse
            D2691E Chocolate
            DC143C Crimson
            00FFFF Aqua/Cyan
            00008B DarkBlue
            008B8B DarkCyan
            B8860B DarkGoldenrod
            A9A9A9 DarkGray
            006400 DarkGreen
            8B008B DarkMagenta
            FF8C00 DarkOrange
            9932CC DarkOrchid
            8B0000 DarkRed
            00CED1 DarkTurquoise -
            9400D3 DarkViolet
            FF1493 DeepPink
            00BFFF DeepSkyBlue 
            696969 DimGray
            1E90FF DodgerBlue 
            B22222 Firebrick 
            228B22 ForestGreen 
            FF00FF Magenta/Fuchsia
            FFD700 Gold
            DAA520 Goldenrod 
            808080 Gray
            008000 Green
            4B0082 Indigo 
            7CFC00 LawnGreen
            D3D3D3 LightGray
            20B2AA LightSeaGreen 
            00FF00 Lime 
            32CD32 LimeGreen 
            0000CD MediumBlue
            C71585 MediumVioletRed
            000080 Navy
            808000 Olive
            FFA500 Orange
            FF4500 OrangeRed
            CD853F Peru
            800080 Purple
            FF0000 Red
            4169E1 RoyalBlue
            8B4513 SaddleBrown
            2E8B57 SeaGreen
            A0522D Sienna
            00FF7F SpringGreen
            D2B48C Tan
            008080 Teal
            40E0D0 Turquoise 
            EE82EE Violet
            FFFF00 Yellow
            9ACD32 YellowGreen
        */

        public static List<SerieStyle> PlotStyles = new() {
            new SerieStyle(OxyColors.ForestGreen),
            new SerieStyle(OxyColors.Red),
            new SerieStyle(OxyColors.Blue),

            new SerieStyle(OxyColors.Orange),
            new SerieStyle(OxyColors.Magenta),
            new SerieStyle(OxyColors.DarkTurquoise),

            new SerieStyle(OxyColors.Chocolate),
            new SerieStyle(OxyColors.DarkViolet),
            new SerieStyle(OxyColors.DodgerBlue),
            new SerieStyle(OxyColors.Lime),

            new SerieStyle(OxyColors.DarkGray),
            new SerieStyle(OxyColors.Black),
/*
            new SerieStyle(OxyColors.ForestGreen,   LineStyle.Dot),
            new SerieStyle(OxyColors.Red,           LineStyle.Dot),
            new SerieStyle(OxyColors.Blue,          LineStyle.Dot),

            new SerieStyle(OxyColors.Orange,        LineStyle.Dot),
            new SerieStyle(OxyColors.Magenta,       LineStyle.Dot),
            new SerieStyle(OxyColors.DarkTurquoise, LineStyle.Dot),

            new SerieStyle(OxyColors.Chocolate,     LineStyle.Dot),
            new SerieStyle(OxyColors.DarkViolet,    LineStyle.Dot),
            new SerieStyle(OxyColors.DodgerBlue,    LineStyle.Dot),
            new SerieStyle(OxyColors.Lime,          LineStyle.Dot),

            new SerieStyle(OxyColors.DarkGray,      LineStyle.Dot),
            new SerieStyle(OxyColors.Black,         LineStyle.Dot)
*/
        };

        public string         Name         { get; set; }
        public PlotController Controller   { get; private set; }
        public string         PlotViewType { get; set; }

        public MyPlotModel(string name) : base()
        {
            Name         = name;
            PlotViewType = "frame";
            PlotMargins  = new OxyThickness(45, 0, 8, 35);

            // Setting white background as with transparent (default) image cannot be copied to Clipboard bacause of WPF bug: https://github.com/oxyplot/oxyplot/issues/17
            Background = OxyColors.White;

            Legends.Add(new Legend
            {
                LegendPosition        = LegendPosition.BottomRight,
                LegendPlacement       = LegendPlacement.Outside,
                LegendOrientation     = LegendOrientation.Horizontal,
                LegendMargin          = 0,
                LegendPadding         = 0,
                LegendBackground      = OxyColor.FromAColor(200, OxyColors.White),
                LegendBorder          = OxyColors.Black,
                LegendBorderThickness = 0,
                ShowInvisibleSeries   = false,
                //LegendFont            = "Arial" // todo@ Chineese characters not rendered correctly
            });

            // 0 -- X (time)
            Axes.Add(new TimeSpanAxis
            {
                AbsoluteMaximum    = 1, // Will be adjusted automatically while getting data
                AbsoluteMinimum    = 0,
                Maximum            = 1, // Will be adjusted automatically while getting data
                Minimum            = 0,
                MaximumPadding     = 0,
                MinimumPadding     = 0,
                MinimumRange       = 0.5,
                MajorGridlineColor = OxyColor.FromArgb(40, 0, 0, 139),
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineColor = OxyColor.FromArgb(20, 0, 0, 139),
                MinorGridlineStyle = LineStyle.Dot,
                Position           = AxisPosition.Bottom,
                StringFormat       = "h:mm:ss"
            });

            // 1 -- Y (size/bitrate)
            Axes.Add(new LinearAxis
            {
                AbsoluteMaximum    = 1, // Will be adjusted automatically while getting data
                AbsoluteMinimum    = 0,
                Maximum            = 1, // Will be adjusted automatically while getting data
                Minimum            = 0,
                MaximumPadding     = 0,
                MinimumPadding     = 0,
                MaximumDataMargin  = 5, // Distance above max Y-value to plot's edge
                MinimumRange       = 1,
                MajorGridlineColor = OxyColor.FromArgb(40, 0, 0, 139),
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineColor = OxyColor.FromArgb(20, 0, 0, 139),
                MinorGridlineStyle = LineStyle.Dot,
                Position           = AxisPosition.Left,
                Angle              = 270
            });

            // Customizing controller to show Graph ToolTip on mouse hover instead of mouse click
            // https://stackoverflow.com/a/34899746/4655944
            var controller = new PlotController();
            controller.UnbindMouseDown(OxyMouseButton.Left);
            controller.BindMouseEnter(PlotCommands.HoverSnapTrack);
            Controller = controller;
        }


        public void Redraw()
        {
            InvalidatePlot(true);
        }


        public bool AxisMaximumSet(int index, double? value = null)
        {
            if (index < 0 || index >= Axes.Count) return false;
            if (value == null)
            {
                Axes[index].Maximum         = (index == 0 ? 10 : 1);
                Axes[index].AbsoluteMaximum = Axes[index].Maximum;
                if (index == 0) AxisXStringFormatSet();
                return true;
            }
            else
            {
                if (value > Axes[index].Maximum)
                {
                    Axes[index].Maximum         = (double)value;
                    Axes[index].AbsoluteMaximum = Axes[index].Maximum;
                    if (index == 0) AxisXStringFormatSet();
                    return true;
                }
            }
            return false;
        }


        public static string AxisXStringFormatBuild(double? duration)
        {
            return (duration == null || (double)duration < 60) ? "m:ss" : (((double)duration < 60 * 60) ? "mm:ss" : "h:mm:ss");
        }


        public void AxisXStringFormatSet()
        {
            Axes[0].StringFormat = AxisXStringFormatBuild(Axes[0].Maximum);
        }


        public bool AxisYTitleAndUnitSet(string? plotViewType)
        {
            if (Axes.Count < 2) return false;

            string? title = AxisYTitleBuild(plotViewType);
            string? unit  = AxisYUnitBuild(plotViewType);

            var result = false;
            if (title != Axes[1].Title) {
                Axes[1].Title = title;
                result = true;
            }
            if (unit != Axes[1].Unit)
            {
                Axes[1].Unit = unit;
                result = true;
            }
            return result;
        }


        public static string? AxisYTitleBuild(string? plotType)
        {
            return (plotType?.ToUpper() ?? "") switch
            {
                "FRAME"  => "Frame size",
                "GOP"    => "Bit rate",
                "SECOND" => "Bit rate",
                _        => ""
            };
        }


        public static string? AxisYUnitBuild(string? plotType)
        {
            return (plotType?.ToUpper() ?? "") switch
            {
                "FRAME"  => "kb",
                "GOP"    => "kb/GOP",
                "SECOND" => "kb/s",
                _        => null
            };
        }


        public void AxesRedraw()
        {
            for (int axisIndex = 0; axisIndex < Axes.Count; ++axisIndex) AxisRedraw(axisIndex);
        }


        public void AxisRedraw(int idx)
        {
            ((MyPlotModel)Axes[idx].PlotModel).Redraw();
        }


        public bool IsEmpty()
        {
            foreach (var serie in Series) if (((StairStepSeries)serie).Points.Count > 0) return false;
            return true;
        }


        public StairStepSeries SerieCreate(FileItem file, int? idx)
        {
            var serie = new StairStepSeries
            {
                IsVisible               = file.IsExistsAndEnabled,
                StrokeThickness         = 1.5,
                Title                   = file.FN,
                TrackerFormatString     = TrackerFormatStringBuild(),
                Decimator               = Decimator.Decimate,
                LineJoin                = LineJoin.Miter,
                VerticalStrokeThickness = 0.5,
                VerticalLineStyle       = LineStyle.Dash
            };
            if (idx != null) SerieStyleApply(serie, PlotStyles[(int)idx]);
            return serie;
        }


        public StairStepSeries? SerieGet(int idx)
        {
            //return idx >=0 && idx < Series.Count ? (LineSeries)Series[idx] : null;
            return idx >=0 && idx < Series.Count ? (StairStepSeries)Series[idx] : null;
        }


        public void SerieSet(int? idx, StairStepSeries? serie)
        {
            if (serie == null) return;
            if (idx == null)
            {
                Series.Add(serie);
                idx = Series.Count - 1;
            }
            else
            {
                Series[(int)idx] = serie;
            }
            SerieStyleApply((int)idx);
        }


        public void SerieRedraw(int idx, bool? visible = null, bool force = false)
        {
            var serie = SerieGet(idx);
            if (serie == null) return;
            bool changed = visible != null && serie.IsVisible != (bool)visible;
            if (changed) serie.IsVisible = (visible == true);
            if (changed || force) ((MyPlotModel)serie.PlotModel).Redraw();
        }


        public bool SeriePointAdd(int idx, double x, int y, int? pos = null)
        {
            var serie = SerieGet(idx);
            if (pos == null || (serie?.Points.Count ?? 0) == 0) serie?.Points.Add(new DataPoint(x, y));
            else serie?.Points.Insert((int)pos, new DataPoint(x, y));
            return true;
        }


        public void SeriePointsAdd(int idx, List<DataPoint> dataPoints)
        {
            SerieGet(idx)?.Points.AddRange(dataPoints);
        }


        public void SeriePointsClear(int idx)
        {
            SerieGet(idx)?.Points.Clear();
        }


        private static void SerieStyleApply(StairStepSeries serie, SerieStyle style)
        {
            serie.Color              = style.Color;
            serie.LineStyle          = style.LineStyle;
            serie.MarkerType         = style.MarkerType;
            //serie.MarkerStroke       = style.Color;
            //serie.MarkerFill         = style.Color;
            //serie.MarkerResolution   = 10;
        }


        public void SerieStyleApply(int idx)
        {
            var serie = SerieGet(idx);
            if(serie != null) SerieStyleApply(serie, PlotStyles[idx]); // todo@ check idx range
        }


        public void PlotViewTypeSet(string plotViewType)
        {
            PlotViewType = plotViewType;
            foreach (var serie in Series) serie.TrackerFormatString = TrackerFormatStringBuild();
        }


        private string TrackerFormatStringBuild()
        {
            return "{0}" + System.Environment.NewLine + "Time={2:hh\\:mm\\:ss\\.fff}" + System.Environment.NewLine + "{3}={4:0} " + AxisYUnitBuild(PlotViewType);
        }


        public static IExporter GetExporter(string? ext = null)
        {
            // todo@ Rid off the OxyPlot.Wpf.PngExporter by implementing the required ExportToBitmap functionality
            return (ext?.ToUpper()) switch
            {
                "SVG" => new OxyPlot.SkiaSharp.SvgExporter { Width = 2400, Height = 600 },
                "PDF" => new OxyPlot.SkiaSharp.PdfExporter { Width = 3200, Height = 800 },
                "PNG" => new OxyPlot.SkiaSharp.PngExporter { Width = 3200, Height = 800 },
                _     => new OxyPlot.Wpf.PngExporter { Width = 3200, Height = 800 }       // OxyPlot.SkiaSharp.PngExporter does not have ExportToBitmap that needed to export to Clipboard
            };
        }

    }
}
