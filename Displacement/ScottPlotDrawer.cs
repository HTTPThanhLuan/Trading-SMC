using Microsoft.VisualBasic;
using ScottPlot;
using ScottPlot.Plottable;
using Smc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Trading
{
    public class ScottPlotDrawer
    {
        private readonly List<Candle> candles;
        private readonly FormsPlot formsPlot;
        private readonly List<SwingPoint> swings;
        private Crosshair _crosshair;      
        public ScottPlotDrawer(List<Candle> candles
            , FormsPlot formsPlot
            , List<SwingPoint> swings
            )
        {
            this.candles = candles;
            this.formsPlot = formsPlot;
            this.swings = swings;
        }
        public void DrawCandles()
        {
            formsPlot.Plot.Clear();

            var ohlcs = candles.Select((x, i) => new OHLC(
                open: (double)x.Open,
                high: (double)x.High,
                low: (double)x.Low,
                close: (double)x.Close,
                oaDate: i,
                oaDateSpan: 0.8
            )).ToArray();

            formsPlot.Plot.AddCandlesticks(ohlcs);

            double[] positions = candles
                .Select((x, i) => (double)i)
                .Where(i => i % 10 == 0)
                .ToArray();

            string[] labels = candles
                .Select((x, i) => new { x.Time, i })
                .Where(x => x.i % 10 == 0)
                .Select(x => x.Time.ToString("MM-dd HH:mm"))
                .ToArray();
            ReMapSwingIndexesToChartCandles();
            DrawSwingPoints();
            DrawSwingZigZagLine();


            formsPlot.Plot.XTicks(positions, labels);
            formsPlot.Plot.AxisAuto();
            formsPlot.Refresh();

        }
        private void DrawSwingZigZagLine()
        {
            var orderedSwings = swings
                .OrderBy(x => x.Index)
                .ToList();

            if (orderedSwings.Count < 2)
                return;

            double[] xs = orderedSwings
                .Select(x => (double)x.Index)
                .ToArray();

            double[] ys = orderedSwings
                .Select(x => (double)x.Level)
                .ToArray();

            var line = formsPlot.Plot.AddScatter(
                xs,
                ys,
                color: Color.Blue,
                lineWidth: 2,
                markerSize: 6);

            line.Label = "Swing Structure";
        }


        private void DrawSwingPoints()
        {
            foreach (var swing in swings)
            {
                double x = swing.Index;
                double y = (double)swing.Level;

                var marker = formsPlot.Plot.AddMarker(
                    x,
                    y,
                    swing.Type == SwingType.High
                        ? MarkerShape.filledTriangleDown
                        : MarkerShape.filledTriangleUp,
                    size: 10);

                marker.Text = swing.Type == SwingType.High
                    ? "Swing High"
                    : "Swing Low";
            }
        }

        private void ReMapSwingIndexesToChartCandles()
        {
            for (int i = 0; i < swings.Count; i++)
            {
                var swing = swings[i];

                int chartIndex = candles.FindIndex(c => c.Time == swing.Time);

                if (chartIndex >= 0)
                    swing.Index = chartIndex;
            }
        }

        private void DrawTrendLines()
        {
            var lows = swings
                .Where(x => x.Type == SwingType.Low)
                .TakeLast(2)
                .ToList();

            if (lows.Count == 2)
            {
                formsPlot.Plot.AddLine(
                    x1: lows[0].Index,
                    y1: (double)lows[0].Level,
                    x2: lows[1].Index,
                    y2: (double)lows[1].Level);
            }

            var highs = swings
                .Where(x => x.Type == SwingType.High)
                .TakeLast(2)
                .ToList();

            if (highs.Count == 2)
            {
                formsPlot.Plot.AddLine(
                    x1: highs[0].Index,
                    y1: (double)highs[0].Level,
                    x2: highs[1].Index,
                    y2: (double)highs[1].Level);
            }
        }


        public void EnableCrosshair()
        {
            _crosshair = formsPlot.Plot.AddCrosshair(0, 0);

            _crosshair.HorizontalLine.Color = Color.Gray;
            _crosshair.VerticalLine.Color = Color.Gray;

            formsPlot.MouseMove += FormsPlot_MouseMove;
        }

        public void DisableCrosshair()
        {
            formsPlot.MouseMove -= FormsPlot_MouseMove;
            if (_crosshair != null)
            {
                formsPlot.Plot.Remove(_crosshair);
                _crosshair = null;
            }
            formsPlot.Render();
        }

        private void FormsPlot_MouseMove(object sender, MouseEventArgs e)
        {
            (double mouseX, double mouseY) =
                formsPlot.GetMouseCoordinates();

            _crosshair.X = mouseX;
            _crosshair.Y = mouseY;

            int candleIndex = (int)Math.Round(mouseX);

            if (candleIndex >= 0 &&
                candleIndex < candles.Count)
            {
                var candle = candles[candleIndex];

                //lblInfo.Text =
                //    $"{candle.Time:MM-dd HH:mm}  " +
                //    $"O:{candle.Open}  " +
                //    $"H:{candle.High}  " +
                //    $"L:{candle.Low}  " +
                //    $"C:{candle.Close}";

                _crosshair.X = candleIndex;
                _crosshair.Y = (double)candle.Close;
            }

            formsPlot.Render();
        }

    }

}