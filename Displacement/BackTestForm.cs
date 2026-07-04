using ScottPlot;
using Smc;
using Trading;

namespace Displacement
{
    public partial class BackTestForm : Form
    {
        private DateTime dateStart, dateEnd;
        private string ticker, timeframe;
        private List<Candle> candles;
        private ScottPlotDrawer scottPlotDrawer;
        private bool isCrosshairEnabled = false
            , isSwingsEnabled = false
            , isStructureBreaksEnabled = false
            , isSRZonesEnabled = false
            , isSDZonesEnabled = false
            , isLiquidityZonesEnabled = false
            , isZigZagLineEnabled = false
            , isFvgZonesEnabled = false
            , isOrderBlocksEnabled = false
            , isdtStartOnFocus = false
            , isdtEndOnFocus = false;
        private int swingLength = 5;

        private readonly FmpService _fmp = new("bNwpAsAvIjEKxid7uc5F78XBPmUuW8l2");
        public BackTestForm()
        {
            InitializeComponent();
            FillCriteria();
            EnableControls(false);
        }
        private void FillCriteria()
        {
            txtTicker.Text = "CDNS";
            dtStart.Value = new DateTime(2026, 03, 26);
            dtStart.Format = DateTimePickerFormat.Custom;
            dtEnd.Value = DateTime.Now;
            dtEnd.Format = DateTimePickerFormat.Custom;
            cboTimeframe.Text = "4hour";
            lblTrendResult.Text = string.Empty;
            lblCandleInfo.Text = string.Empty;
            numSwingLength.Value = 5;


        }

        private void EnableControls(bool enable)
        {

            checkCrosshair.Enabled = enable;
            checkSwings.Enabled = enable;
            checkStructureBreaks.Enabled = enable;
            checkSRZones.Enabled = enable;
            checkSDZones.Enabled = enable;
            checkLiquidityZones.Enabled = enable;
            checkZigZagLine.Enabled = enable;
            checkFvgZones.Enabled = enable;
            checkOrderBlocks.Enabled = enable;
        }

        private void BackTestForm_Load(object sender, EventArgs e)
        {
            // Initialize form on load if needed
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            await DrawChart();
            EnableControls(true);
        }

        private async Task DrawChart()
        {
            BindSelectedCriteriaToVariables();
            await fetchDataMarket();
            candles = candles.Where(candle => candle.Time >= dateStart && candle.Time <= dateEnd).ToList();

            scottPlotDrawer = new ScottPlotDrawer(candles, spChart, timeframe, onMoveOnChart, onDoubleClickOnCandle, swingLength);
            scottPlotDrawer.DrawCandles();

            lblTrendResult.Text = $"{scottPlotDrawer.GetTrend()}";
            DrawingIndicators();
        }

        private void onMoveOnChart(Candle candle)
        {
            lblCandleInfo.Text =
                $"{candle.Time:MM-dd HH:mm}  " +
                $"O:{candle.Open}  " +
                $"H:{candle.High}  " +
                $"L:{candle.Low}  " +
                $"C:{candle.Close}";
        }

        private void onDoubleClickOnCandle(Candle candle)
        {
            if (isdtStartOnFocus)
            {
                dtStart.Value = candle.Time;
                dtStart.Focus();
            }
            else if (isdtEndOnFocus)
            {
                dtEnd.Value = candle.Time;
                dtEnd.Focus();
            }
        }

      

        private void BindSelectedCriteriaToVariables()
        {
            ticker = txtTicker.Text;
            dateStart = dtStart.Value;
            dateEnd = dtEnd.Value;
            timeframe = cboTimeframe.SelectedItem.ToString();
            swingLength = (int)numSwingLength.Value;
        }

        private async Task fetchDataMarket()
        {
            candles = await _fmp.GetCandlesAsync(ticker, timeframe, dateStart, dateEnd);
        }

        private void checkCrosshair_CheckedChanged(object sender, EventArgs e)
        {
            isCrosshairEnabled = checkCrosshair.Checked;
            if (checkCrosshair.Checked)
                scottPlotDrawer.EnableCrosshair();
            else
                scottPlotDrawer.DisableCrosshair();
        }

        private async void cboTimeframe_SelectedIndexChanged(object sender, EventArgs e)
        {
            timeframe = cboTimeframe.SelectedItem.ToString();
            if (string.IsNullOrEmpty(ticker))
                return;

            await DrawChart();
         
        }

        private void DrawingIndicators()
        {
            if (scottPlotDrawer == null) return;

            if (isSwingsEnabled)
                scottPlotDrawer.AddSwings();
            else
                scottPlotDrawer.RemoveSwings();
            if (isZigZagLineEnabled)
                scottPlotDrawer.AddZigZagLine();
            else
                scottPlotDrawer.RemoveZigZagLine();
            if (isSDZonesEnabled)
                scottPlotDrawer.AddSDZones();
            else
                scottPlotDrawer.RemoveSDZones();
            if (isSRZonesEnabled)
                scottPlotDrawer.AddSRZones();
            else
                scottPlotDrawer.RemoveSRZones();
            if (isFvgZonesEnabled)
                scottPlotDrawer.AddFvgZones();
            else
                scottPlotDrawer.RemoveFvgZones();
            if (isOrderBlocksEnabled)
                scottPlotDrawer.AddOrderBlockZones();
            else
                scottPlotDrawer.RemoveOrderBlockZones();
            if (isLiquidityZonesEnabled)
                scottPlotDrawer.AddLiquidityZones();
            else
                scottPlotDrawer.RemoveLiquidityZones();
            if (isStructureBreaksEnabled)
                scottPlotDrawer.AddStructureBreaks();
            else
                scottPlotDrawer.RemoveStructureBreaks();

            if(isCrosshairEnabled)
                scottPlotDrawer.EnableCrosshair();
            else
                scottPlotDrawer.DisableCrosshair();
        }

        private void checkSwings_CheckedChanged(object sender, EventArgs e)
        {
            if (scottPlotDrawer == null) return;

            isSwingsEnabled = checkSwings.Checked;

            if (isSwingsEnabled)
                scottPlotDrawer.AddSwings();
            else
                scottPlotDrawer.RemoveSwings();
        }

        private void checkZigZagLine_CheckedChanged(object sender, EventArgs e)
        {
            if (scottPlotDrawer == null) return;

            isZigZagLineEnabled = checkZigZagLine.Checked;

            if (isZigZagLineEnabled)
                scottPlotDrawer.AddZigZagLine();
            else
                scottPlotDrawer.RemoveZigZagLine();
        }

        private void checkSDZones_CheckedChanged(object sender, EventArgs e)
        {
            if (scottPlotDrawer == null) return;

            isSDZonesEnabled = checkSDZones.Checked;

            if (isSDZonesEnabled)
                scottPlotDrawer.AddSDZones();
            else
                scottPlotDrawer.RemoveSDZones();
        }

        private void checkSRZones_CheckedChanged(object sender, EventArgs e)
        {
            if (scottPlotDrawer == null) return;

            isSRZonesEnabled = checkSRZones.Checked;

            if (isSRZonesEnabled)
                scottPlotDrawer.AddSRZones();
            else
                scottPlotDrawer.RemoveSRZones();
        }

        private void checkFvgZones_CheckedChanged(object sender, EventArgs e)
        {
            if (scottPlotDrawer == null) return;

            isFvgZonesEnabled = checkFvgZones.Checked;

            if (isFvgZonesEnabled)
                scottPlotDrawer.AddFvgZones();
            else
                scottPlotDrawer.RemoveFvgZones();
        }

        private void checkOrderBlocks_CheckedChanged(object sender, EventArgs e)
        {
            if (scottPlotDrawer == null) return;

            isOrderBlocksEnabled = checkOrderBlocks.Checked;

            if (isOrderBlocksEnabled)
                scottPlotDrawer.AddOrderBlockZones();
            else
                scottPlotDrawer.RemoveOrderBlockZones();
        }

        private void checkLiquidityZones_CheckedChanged(object sender, EventArgs e)
        {
            if (scottPlotDrawer == null) return;

            isLiquidityZonesEnabled = checkLiquidityZones.Checked;

            if (isLiquidityZonesEnabled)
                scottPlotDrawer.AddLiquidityZones();
            else
                scottPlotDrawer.RemoveLiquidityZones();
        }

        private void checkStructureBreaks_CheckedChanged(object sender, EventArgs e)
        {
            if (scottPlotDrawer == null) return;

            isStructureBreaksEnabled = checkStructureBreaks.Checked;

            if (isStructureBreaksEnabled)
                scottPlotDrawer.AddStructureBreaks();
            else
                scottPlotDrawer.RemoveStructureBreaks();
        }



        private void dtStart_Leave(object sender, EventArgs e)
        {
           // isdtStartOnFocus = false;
        }

        private void dtStart_Enter(object sender, EventArgs e)
        {
            isdtStartOnFocus = true;
            isdtEndOnFocus = false;
        }

        private void dtEnd_Enter(object sender, EventArgs e)
        {
            isdtEndOnFocus = true;
            isdtStartOnFocus = false;
        }

        private void dtEnd_Leave(object sender, EventArgs e)
        {
           // isdtEndOnFocus = false;
        }

        private void spChart_DoubleClick(object sender, EventArgs e)
        {
            var (mouseX, mouseY) = spChart.GetMouseCoordinates();

            int candleIndex = (int)Math.Round(mouseX);

            if (candleIndex < 0 || candleIndex >= candles.Count)
                return;

            var candle = candles[candleIndex];

            onDoubleClickOnCandle(candle);

        }
    }
}
