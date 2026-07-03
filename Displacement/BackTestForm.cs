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
        private bool isCrosshairEnabled = false, isSwingsEnabled = false, isStructureBreaksEnabled = false
            , isSRZonesEnabled = false, isSDZonesEnabled = false
            , isLiquidityZonesEnabled = false, isZigZagLineEnabled = false, isFvgZonesEnabled = false, isOrderBlocksEnabled = false;

        private readonly FmpService _fmp = new("bNwpAsAvIjEKxid7uc5F78XBPmUuW8l2");
        public BackTestForm()
        {
            InitializeComponent();
            FillCriteria();
        }
        private void FillCriteria()
        {
            txtTicker.Text = "CDNS";
            dtStart.Value = new DateTime(2026, 03, 26);
            dtEnd.Value = DateTime.Now;
            cboTimeframe.Text = "4hour";
        }

        private void BackTestForm_Load(object sender, EventArgs e)
        {
            // Initialize form on load if needed
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            await DrawChart();
        }

        private async Task DrawChart()
        {
            BindSelectedCriteriaToVariables();
            await fetchDataMarket();
            scottPlotDrawer = new ScottPlotDrawer(candles, spChart, timeframe);
            scottPlotDrawer.DrawCandles();
        }

        private void cboView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var zoneResult = new ZoneResultForm();
            zoneResult.Text = $"{cboView.SelectedItem.ToString()} - {txtTicker.Text}";
            zoneResult.Show();
        }

        private void BindSelectedCriteriaToVariables()
        {
            ticker = txtTicker.Text;
            dateStart = dtStart.Value;
            dateEnd = dtEnd.Value;
            timeframe = cboTimeframe.SelectedItem.ToString();
        }

        private async Task fetchDataMarket()
        {
            candles = await _fmp.GetCandlesAsync(ticker, timeframe, dateStart, dateEnd);
        }

        private void checkCrosshair_CheckedChanged(object sender, EventArgs e)
        {
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
    }
}
