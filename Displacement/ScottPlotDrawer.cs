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
    
        public List<IPlottable> fvgZones = new(), swingMarkers = new()
            , zigZagLine = new(), supplyDemandZones = new()
            , liquidityZones = new()
            , supportResistanceZones = new(), orderBlockZones = new(), structureBreaks = new();

        private readonly List<Candle> candles;
        private readonly FormsPlot formsPlot;
        private string timeframe;
        private Crosshair _crosshair;
        private SmcAnalyzer smcAnalyzer;
        private SmcAnalysisResult smcAnalysisResult;
        private Action<Candle> onMoveOnCandleCallBack;
        private Action<Candle> onDoubleClickOnCandleCallBack;
        public ScottPlotDrawer(List<Candle> candles
            , FormsPlot formsPlot
            , string timeframe
            , Action<Candle> onMoveOnCandleCallBack
            , Action<Candle> onDoubleClickOnCandleCallBack
            )
        {
            this.candles = candles;
            this.formsPlot = formsPlot;
            this.timeframe = timeframe;
            this.onMoveOnCandleCallBack = onMoveOnCandleCallBack;
            this.onDoubleClickOnCandleCallBack = onDoubleClickOnCandleCallBack;

            smcAnalyzer = new SmcAnalyzer();
            smcAnalysisResult = smcAnalyzer.Analyze(
                candles,
                range: candles.Count,
                swingLength: 7,
                sourceTimeFrame: timeframe
            );

            //  formsPlot.MouseDoubleClick -= FormsPlot_MouseDoubleClick;
            //this.formsPlot.Controls[0].DoubleClick += FormsPlot_MouseDoubleClick;
        }

        private void FormsPlot_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            var (mouseX, mouseY) = formsPlot.GetMouseCoordinates();

            int candleIndex = (int)Math.Round(mouseX);

            if (candleIndex < 0 || candleIndex >= candles.Count)
                return;

            var candle = candles[candleIndex];

            onDoubleClickOnCandleCallBack?.Invoke(candle);


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
            // DrawSwingPoints();
            //DrawSwingZigZagLine();
            // DrawTrendLines();
            //DrawStructureBreaks();
            //DrawSupportResistanceZones();
            //DrawSupplyDemandZones();
            //DrawLiquidityZones();

            formsPlot.Plot.XTicks(positions, labels);
            formsPlot.Plot.AxisAuto();
            formsPlot.Refresh();

        }
        private void DrawSwingZigZagLine()
        {
            zigZagLine.Clear();
            var orderedSwings = smcAnalysisResult.Swings
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
            zigZagLine.Add(line);
        }


        //private void DrawSwingPoints()
        //{
        //    foreach (var swing in swings)
        //    {
        //        double x = swing.Index;
        //        double y = (double)swing.Level;

        //        var marker = formsPlot.Plot.AddMarker(
        //            x,
        //            y,
        //            swing.Type == SwingType.High
        //                ? MarkerShape.filledTriangleDown
        //                : MarkerShape.filledTriangleUp,
        //            size: 10);

        //        marker.Text = swing.Type == SwingType.High
        //            ? "Swing High"
        //            : "Swing Low";
        //    }
        //}

        private void DrawSwingPoints()
        {
            swingMarkers.Clear();
            // keep track of previous same-type swing
            SwingPoint? previousHigh = null;
            SwingPoint? previousLow = null;

            foreach (var swing in smcAnalysisResult.Swings.OrderBy(x => x.Index))
            {
                double x = swing.Index;
                double y = (double)swing.Level;

                string label = "";

                if (swing.Type == SwingType.High)
                {
                    if (previousHigh == null)
                        label = "H";   // first high
                    else
                        label = swing.Level > previousHigh.Level
                            ? "HH"
                            : "LH";

                    previousHigh = swing;
                }
                else
                {
                    if (previousLow == null)
                        label = "L";   // first low
                    else
                        label = swing.Level > previousLow.Level
                            ? "HL"
                            : "LL";

                    previousLow = swing;
                }

                var marker = formsPlot.Plot.AddMarker(
                    x,
                    y,
                    swing.Type == SwingType.High
                        ? MarkerShape.filledTriangleDown
                        : MarkerShape.filledTriangleUp,
                    size: 10);

                marker.Text = label;

                marker.Color = label switch
                {
                    "HH" => Color.Green,
                    "HL" => Color.LightGreen,
                    "LH" => Color.Orange,
                    "LL" => Color.Red,
                    _ => Color.Blue
                };
                swingMarkers.Add(marker);
            }
        }

        private void ReMapSwingIndexesToChartCandles()
        {
            for (int i = 0; i < smcAnalysisResult.Swings.Count; i++)
            {
                var swing = smcAnalysisResult.Swings[i];

                int chartIndex = candles.FindIndex(c => c.Time == swing.Time);

                if (chartIndex >= 0)
                    swing.Index = chartIndex;
            }
        }

        private void DrawTrendLines()
        {
            var lows = smcAnalysisResult.Swings
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

            var highs = smcAnalysisResult.Swings
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
            formsPlot.MouseMove -= FormsPlot_MouseMove;
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
                //_crosshair.HorizontalLine.Label = $"C:{candle.Close}";
                //_crosshair.VerticalLine.Label = $"{candle.Time:MM-dd HH:mm}";
                //_crosshair.Label = $"Candle {candleIndex}";

                onMoveOnCandleCallBack?.Invoke(candle);
            }

            formsPlot.Render();
        }

        private void DrawStructureBreaks()
        {
            structureBreaks.Clear();
            foreach (var sb in smcAnalysisResult.StructureBreaks)
            {
                if (sb.BrokenIndex <= 0)
                    continue;

                double x1 = sb.Index;
                double x2 = sb.BrokenIndex;
                double y = (double)sb.Level;

                var color = sb.Direction == Direction.Bullish
                    ? Color.Green
                    : Color.Red;

                string label = sb.IsChoch ? "CHoCH" : "BOS";

                // horizontal level line
                var line = formsPlot.Plot.AddLine(
                    x1: x1,
                    y1: y,
                    x2: x2,
                    y2: y,
                    color: color,
                    lineWidth: 2);
                structureBreaks.Add(line);

                line.LineStyle = LineStyle.Dash;

                // vertical line at broken candle
                var vline = formsPlot.Plot.AddVerticalLine(
                    x: x2,
                    color: color,
                    width: 1);

                vline.LineStyle = LineStyle.Dot;
                structureBreaks.Add(vline);
                // marker at break point
                var marker = formsPlot.Plot.AddMarker(
                    x2,
                    y,
                    sb.Direction == Direction.Bullish
                        ? MarkerShape.filledCircle
                        : MarkerShape.openCircle,
                    size: 8,
                    color: color);
                structureBreaks.Add(marker);

                // text label
                var text = formsPlot.Plot.AddText(
                    label,
                    (x1 + x2) / 2,
                    y);

                text.Color = color;
                text.FontSize = 12;
                text.Alignment = Alignment.LowerCenter;
                structureBreaks.Add(text);
            }
        }

        private void DrawSupportResistanceZones()
        {
            supportResistanceZones.Clear();
            if (candles == null || candles.Count == 0)
                return;

            double x1 = 0;
            double x2 = candles.Count - 1;

            foreach (var zone in smcAnalysisResult.SupportResistanceZones)
            {
                if (zone.Type != ZoneType.Support &&
                    zone.Type != ZoneType.Resistance)
                    continue;

                double yTop = (double)zone.High;
                double yBottom = (double)zone.Low;

                var color = zone.Type == ZoneType.Support
                    ? Color.Green
                    : Color.Red;

                var rect = formsPlot.Plot.AddRectangle(
                    x1,
                    x2,
                    yBottom,
                    yTop);

                rect.BorderColor = color;
                rect.BorderLineWidth = 1;
                rect.Color = Color.FromArgb(35, color);
                supportResistanceZones.Add(rect);

                var text = formsPlot.Plot.AddText(
                    zone.Type.ToString(),
                    x2,
                    (yTop + yBottom) / 2);

                text.Color = color;
                text.FontSize = 10;
                text.Alignment = Alignment.MiddleRight;
                supportResistanceZones.Add(text);
            }
        }

        private void DrawSupplyDemandZones()
        {
            supplyDemandZones.Clear();
            if (candles == null || candles.Count == 0)
                return;

            foreach (var zone in smcAnalysisResult.SupplyDemandZones)
            {
                if (zone.Type != ZoneType.Demand &&
                    zone.Type != ZoneType.Supply)
                    continue;

                double x1 = zone.StartIndex > 0 ? zone.StartIndex : 0;
                double x2 = candles.Count - 1;

                double yTop = (double)zone.High;
                double yBottom = (double)zone.Low;

                var color = zone.Type == ZoneType.Demand
                    ? Color.Green
                    : Color.Red;

                var rect = formsPlot.Plot.AddRectangle(
                    x1,
                    x2,
                    yBottom,
                    yTop);               

                rect.BorderColor = color;
                rect.BorderLineWidth = 2;
                rect.Color = Color.FromArgb(45, color);
                supplyDemandZones.Add(rect);

                var text = formsPlot.Plot.AddText(
                    zone.Type == ZoneType.Demand ? "Demand" : "Supply",
                    x2,
                    (yTop + yBottom) / 2);

                text.Color = color;
                text.FontSize = 10;
                text.Alignment = Alignment.MiddleRight;
                supplyDemandZones.Add(text);
            }
        }

        private void DrawLiquidityZones()
        {
            if (candles == null || candles.Count == 0)
                return;

            foreach (var zone in smcAnalysisResult.LiquidityZones)
            {
                if (zone.Type != ZoneType.BullishLiquidity &&
                    zone.Type != ZoneType.BearishLiquidity)
                    continue;

                double x1 = zone.StartIndex;
                double x2 = zone.SweptIndex.HasValue && zone.SweptIndex.Value > 0
                    ? zone.SweptIndex.Value
                    : candles.Count - 1;

                double yBottom = (double)zone.Low;
                double yTop = (double)zone.High;
                double yMid = (yBottom + yTop) / 2;

                var color = zone.Type == ZoneType.BullishLiquidity
                    ? Color.Red      // liquidity above swing highs
                    : Color.Green;   // liquidity below swing lows

                var rect = formsPlot.Plot.AddRectangle(
                    x1,
                    x2,
                    yBottom,
                    yTop);

                rect.BorderColor = color;
                rect.BorderLineWidth = 1;
                rect.Color = Color.FromArgb(35, color);

                var line = formsPlot.Plot.AddLine(
                    x1: x1,
                    y1: yMid,
                    x2: x2,
                    y2: yMid,
                    color: color,
                    lineWidth: 1);

                line.LineStyle = LineStyle.Dash;

                string label = zone.Type == ZoneType.BullishLiquidity
                    ? $"Buy-side Liq ({zone.TouchCount})"
                    : $"Sell-side Liq ({zone.TouchCount})";

                var text = formsPlot.Plot.AddText(
                    label,
                    x2,
                    yMid);

                text.Color = color;
                text.FontSize = 10;
                text.Alignment = Alignment.MiddleRight;

                if (zone.SweptIndex.HasValue && zone.SweptIndex.Value > 0)
                {
                    formsPlot.Plot.AddMarker(
                        zone.SweptIndex.Value,
                        yMid,
                        MarkerShape.filledCircle,
                        size: 7,
                        color: color);

                    var sweptText = formsPlot.Plot.AddText(
                        "Swept",
                        zone.SweptIndex.Value,
                        yMid);

                    sweptText.Color = color;
                    sweptText.FontSize = 9;
                    sweptText.Alignment = Alignment.UpperCenter;
                }
            }
        }

        private void DrawFvgZones()
        {
            if (candles == null || candles.Count == 0)
                return;

            foreach (var zone in smcAnalysisResult.FvgZones)
            {
                if (zone.Type != ZoneType.BullishFvg &&
                    zone.Type != ZoneType.BearishFvg)
                    continue;

                double x1 = zone.StartIndex;
                double x2 = zone.MitigatedIndex.HasValue && zone.MitigatedIndex.Value > 0
                    ? zone.MitigatedIndex.Value 
                    : candles.Count - 1;

                double yBottom = (double)zone.Low;
                double yTop = (double)zone.High;
                double yMid = (yBottom + yTop) / 2;

                var color = zone.Type == ZoneType.BullishFvg
                    ? Color.Green
                    : Color.Red;

                var rect = formsPlot.Plot.AddRectangle(
                    x1,
                    x2,
                    yBottom,
                    yTop);

                rect.BorderColor = color;
                rect.BorderLineWidth = 1;
                rect.Color = Color.FromArgb(35, color);

                fvgZones.Add(rect);

                string label = zone.Type == ZoneType.BullishFvg
                    ? "Bullish FVG"
                    : "Bearish FVG";

                if (zone.MitigatedIndex > 0)
                    label += " Mitigated";

                var text = formsPlot.Plot.AddText(
                    label,
                    x2,
                    yMid);

                text.Color = color;
                text.FontSize = 9;
                text.Alignment = Alignment.MiddleRight;

                fvgZones.Add(text);
            }
        }

        private void DrawOrderBlockZones()
        {
            if (candles == null || candles.Count == 0)
                return;

            foreach (var zone in smcAnalysisResult.OrderBlocks)
            {
                if (zone.Type != ZoneType.BullishOrderBlock &&
                    zone.Type != ZoneType.BearishOrderBlock)
                    continue;

                double x1 = zone.StartIndex;
                double x2 = zone.MitigatedIndex.HasValue && zone.MitigatedIndex.Value > 0
                    ? zone.MitigatedIndex.Value
                    : candles.Count - 1;

                double yBottom = (double)zone.Low;
                double yTop = (double)zone.High;
                double yMid = (yBottom + yTop) / 2;

                var color = zone.Type == ZoneType.BullishOrderBlock
                    ? Color.Green
                    : Color.Red;

                var rect = formsPlot.Plot.AddRectangle(
                    x1,
                    x2,
                    yBottom,
                    yTop);

                rect.BorderColor = color;
                rect.BorderLineWidth = 2;
                rect.Color = Color.FromArgb(45, color);

                orderBlockZones.Add(rect);

                string label = zone.Type == ZoneType.BullishOrderBlock
                    ? "Bullish OB"
                    : "Bearish OB";

                if (zone.MitigatedIndex > 0)
                    label += " Mitigated";

                var text = formsPlot.Plot.AddText(
                    label,
                    x2,
                    yMid);

                text.Color = color;
                text.FontSize = 9;
                text.Alignment = Alignment.MiddleRight;

                orderBlockZones.Add(text);

                var marker = formsPlot.Plot.AddMarker(
                    x1,
                    yMid,
                    MarkerShape.filledSquare,
                    size: 7,
                    color: color);

                marker.Text = "OB";

                orderBlockZones.Add(marker);
            }
        }

        public string GetTrend()
        {
           return $"Current Trend is {smcAnalysisResult.Trend.ToString()}";
        }

        public void AddSwings()
        {
            DrawSwingPoints();
            formsPlot.Render();
        }
        public void RemoveSwings()
        {
            foreach (var marker in swingMarkers)
            {
                formsPlot.Plot.Remove(marker);
            }

            swingMarkers.Clear();

            formsPlot.Render();
        }

        public void AddZigZagLine()
        {
            DrawSwingZigZagLine();
            formsPlot.Render();
        }
        public void RemoveZigZagLine()
        {
            foreach (var marker in zigZagLine)
            {
                formsPlot.Plot.Remove(marker);
            }
            zigZagLine.Clear();
            formsPlot.Render();
        }

        public void AddSDZones()
        {
            DrawSupplyDemandZones();
            formsPlot.Render();
        }
        public void RemoveSDZones()
        {
            foreach (var zone in supplyDemandZones)
            {
                formsPlot.Plot.Remove(zone);
            }
            supplyDemandZones.Clear();
            formsPlot.Render();
        }

        public void AddSRZones()
        {
            DrawSupportResistanceZones();
            formsPlot.Render();
        }
        public void RemoveSRZones()
        {
            foreach(var zone in supportResistanceZones)
            {
                formsPlot.Plot.Remove(zone);
            }
            supportResistanceZones.Clear();
            formsPlot.Render();
        }

        public void AddFvgZones()
        {
            DrawFvgZones();
            formsPlot.Render();
        }
        public void RemoveFvgZones()
        {
            foreach (var zone in fvgZones)
            {
                formsPlot.Plot.Remove(zone);
            }
            fvgZones.Clear();
            formsPlot.Render();
        }

        public void AddOrderBlockZones()
        {
            DrawOrderBlockZones();
            formsPlot.Render();
        }
        public void RemoveOrderBlockZones()
        {
            foreach (var zone in orderBlockZones)
            {
                formsPlot.Plot.Remove(zone);
            }
            orderBlockZones.Clear();
            formsPlot.Render();
        }

        public void AddLiquidityZones()
        {
            DrawLiquidityZones();
            formsPlot.Render();
        }
        public void RemoveLiquidityZones()
        {
            foreach (var zone in liquidityZones)
            {
                formsPlot.Plot.Remove(zone);
            }
            liquidityZones.Clear();
            formsPlot.Render();
        }

        public void AddStructureBreaks()
        {
            DrawStructureBreaks();
            formsPlot.Render();
        }
        public void RemoveStructureBreaks()
        {
            foreach (var sb in structureBreaks)
            {
                formsPlot.Plot.Remove(sb);
            }
            structureBreaks.Clear();
            formsPlot.Render();
        }
    }
}