using Smc;
using Trading;

namespace Displacement
{
    public partial class BackTestForm : Form
    {
        private DateTime dateStart, dateEnd;
        private string ticker;
        private List<Candle> candles;
        private ScottPlotDrawer scottPlotDrawer;
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
        }

        private void BackTestForm_Load(object sender, EventArgs e)
        {
            // Initialize form on load if needed
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            BindSelectedCriteriaToVariables();
            await fetchDataMarket();

            var analyzer = new SmcAnalyzer();
            var result = analyzer.Analyze(
                candles,
                range: 130,
                swingLength: 5,
                sourceTimeFrame: "4hour"
            );

            scottPlotDrawer = new ScottPlotDrawer(candles, spChart, result.Swings);
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
        }

        private async Task fetchDataMarket()
        {
            candles = await _fmp.GetCandlesAsync(ticker, "4hour", dateStart, dateEnd);
        }

        private void checkCrosshair_CheckedChanged(object sender, EventArgs e)
        {
            if (checkCrosshair.Checked)
                scottPlotDrawer.EnableCrosshair();
            else
                scottPlotDrawer.DisableCrosshair();
        }
    }
}
