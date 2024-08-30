using CsvExporter;
using IISCheck.EngineIISCheck;
using IISCheck.Model;
using Logger;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MainApp
{
    public partial class Main : Form
    {
        private List<string> monitoredPools = new List<string>();
        private int monitoredAppCount = 0;

        private readonly Logger.Logger _logger;
        private IISMonitoringService _monitoringService;

        private Timer countdownTimer;
        private int countdownTime; // in seconds
        private int refreshInterval = 10; // Set this to your desired refresh interval (in seconds)

        // Drag panel
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public Main()
        {
            InitializeComponent();
            UIsetUp();
            _logger = new Logger.Logger(new FileLogRepository("Application.log"));
        }

        private void UpdateDataGridView(List<IISApplicationPoolModel> appPoolModels)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateDataGridView(appPoolModels)));
                return;
            }

            dataGridViewAppPools.Rows.Clear();

            foreach (var pool in appPoolModels)
            {
                dataGridViewAppPools.Rows.Add(pool.Name, pool.Status, pool.CPUUsage, pool.MemoryUsage / (1024 * 1024), pool.RequestCount);
            }

            CsvExporter.ICsvExporter csvExporter = new CsvExporter.CsvExporter();
            csvExporter.ExportToCsv(appPoolModels);

            // Reapply the colors to the monitored rows
            ReapplyRowColors();

            // Update labels with current counts
            labelTotallabel.Text = $"{appPoolModels.Count}";
            labelMonitoring.Text = $"{monitoredAppCount}";
        }
        private void ReapplyRowColors()
        {
            foreach (DataGridViewRow row in dataGridViewAppPools.Rows)
            {
                var poolName = row.Cells["Name"].Value.ToString();
                if (monitoredPools.Contains(poolName))
                {
                    ChangeRowColor(row.Index, Color.FromArgb(85, 139, 47)); 
                }
            }
        }
        private void InitializeDataGridView()
        {
            // Clear existing columns (if any)
            dataGridViewAppPools.Columns.Clear();

            // Set basic appearance properties with dark tones
            dataGridViewAppPools.BorderStyle = BorderStyle.None;
            dataGridViewAppPools.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48); // Dark gray for alternate rows
            dataGridViewAppPools.AlternatingRowsDefaultCellStyle.ForeColor = Color.White; // White text for alternate rows
            dataGridViewAppPools.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewAppPools.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30); // Dark background for cells
            dataGridViewAppPools.DefaultCellStyle.ForeColor = Color.White; // White text for cells
            dataGridViewAppPools.DefaultCellStyle.SelectionBackColor = Color.FromArgb(63, 63, 70); // Slightly lighter dark gray for selected row
            dataGridViewAppPools.DefaultCellStyle.SelectionForeColor = Color.White;
            dataGridViewAppPools.BackgroundColor = Color.FromArgb(30, 30, 30); // Dark background

            dataGridViewAppPools.EnableHeadersVisualStyles = false;
            dataGridViewAppPools.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewAppPools.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 28, 28); // Very dark gray for header
            dataGridViewAppPools.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            // Set row height and other properties
            dataGridViewAppPools.RowTemplate.Height = 40;
            dataGridViewAppPools.AllowUserToAddRows = false;
            dataGridViewAppPools.AllowUserToDeleteRows = false;
            dataGridViewAppPools.ReadOnly = true;

            // Add columns
            dataGridViewAppPools.Columns.Add("Name", "Application Pool Name");
            dataGridViewAppPools.Columns.Add("Status", "Status");
            dataGridViewAppPools.Columns.Add("CPUUsage", "CPU Usage (%)");
            dataGridViewAppPools.Columns.Add("MemoryUsage", "Memory Usage (MB)");
            dataGridViewAppPools.Columns.Add("RequestCount", "Request Count");

            // Add a "Monitor" button column
            DataGridViewButtonColumn monitorButtonColumn = new DataGridViewButtonColumn
            {
                HeaderText = "Monitor",
                Text = "Start Monitoring",
                UseColumnTextForButtonValue = true,
                Name = "MonitorButton",
                FlatStyle = FlatStyle.Flat, // Flat style for a modern look
                DefaultCellStyle = { BackColor = Color.FromArgb(28, 28, 28), ForeColor = Color.White } // Button style
            };
            dataGridViewAppPools.Columns.Add(monitorButtonColumn);

            // Set flat style for the entire grid with custom font and alignment
            foreach (DataGridViewColumn col in dataGridViewAppPools.Columns)
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.DefaultCellStyle.Font = new Font("Tahoma", 10);
                col.DefaultCellStyle.ForeColor = Color.White; // Ensure text is white
            }

            // Ensure the DataGridView fits within its container
            dataGridViewAppPools.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewAppPools.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop real-time monitoring when the form is closing
            _monitoringService.StopRealTimeMonitoring();
        }
        private void Main_Load(object sender, EventArgs e)
        {
            InitializeDataGridView();

            // Initialize the monitoring service with a log file path
            _monitoringService = new IISMonitoringService("IISRealTimeMonitoring.log");

            // Subscribe to the monitoring data updated event
            _monitoringService.OnMonitoringDataUpdated += UpdateDataGridView;

            // Start real-time monitoring with a 10-second interval
            _monitoringService.StartRealTimeMonitoring(10000);

            // Initialize the countdown timer
            countdownTime = refreshInterval;
            countdownTimer = new Timer();
            countdownTimer.Interval = 1000; // 1 second
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            if (countdownTime > 0)
            {
                countdownTime--;
                labelCountdown.Text = $"Next refresh in: {countdownTime} seconds";
            }
            else
            {
                // Reset the countdown after each refresh
                countdownTime = refreshInterval;
            }
        }

        private void dataGridViewAppPools_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dataGridViewAppPools.Columns["MonitorButton"].Index)
            {
                // Get the selected application pool's name from the row
                var poolName = dataGridViewAppPools.Rows[e.RowIndex].Cells["Name"].Value.ToString();

                // Start monitoring the selected application pool
                StartMonitoringPool(poolName);

                // Add the pool name to the monitored list if not already added
                if (!monitoredPools.Contains(poolName))
                {
                    monitoredPools.Add(poolName);
                    monitoredAppCount++;
                }

                // Reapply colors to all rows after refresh
                ReapplyRowColors();
            }
        }
        private void ChangeRowColor(int rowIndex, Color color)
        {
            foreach (DataGridViewCell cell in dataGridViewAppPools.Rows[rowIndex].Cells)
            {
                cell.Style.BackColor = color;
            }
        }
        private void StartMonitoringPool(string poolName)
        {
            _logger.LogInfo($"Starting monitoring for Application Pool '{poolName}'.");

            // Start real-time monitoring for the specific pool with a defined interval (e.g., 5 seconds)
            _monitoringService.StartRealTimeMonitoringForSpecificPool(poolName, 10000);

            MessageBox.Show($"Monitoring started for Application Pool '{poolName}'.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void UIsetUp()
        {
            this.panelTop.MouseDown += new MouseEventHandler(Panel_MouseDown);
            this.panelTop.MouseMove += new MouseEventHandler(Panel_MouseMove);
            this.panelTop.MouseUp += new MouseEventHandler(Panel_MouseUp);
        }
        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }
        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(dif));
            }
        }
        private void Panel_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }
    }
}
