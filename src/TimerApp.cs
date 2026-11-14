using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

internal sealed class TimerWindow : Form
{
    private readonly Stopwatch clock = new Stopwatch();
    private readonly System.Windows.Forms.Timer ticker = new System.Windows.Forms.Timer();
    private readonly List<long> laps = new List<long>();
    private readonly string dataPath;
    private long lastLap;
    private long elapsedOffset;
    private long ElapsedMilliseconds { get { return elapsedOffset + clock.ElapsedMilliseconds; } }
    private bool running;
    private readonly Label display = new Label();
    private readonly Label status = new Label();
    private readonly Button startButton = new Button();
    private readonly Button lapButton = new Button();
    private readonly Button resetButton = new Button();
    private readonly ListView lapList = new ListView();

    public TimerWindow()
    {
        Text = "精确计时器";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(500, 530);
        MinimumSize = new Size(420, 500);
        BackColor = Color.FromArgb(246, 248, 250);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "timer-data.json");

        var heading = new Label { Text = "精确计时器", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(32, 44, 56), AutoSize = true, Location = new Point(28, 24) };
        status.Text = "毫秒计时  ·  已就绪";
        status.Font = new Font("Segoe UI", 9F);
        status.ForeColor = Color.FromArgb(105, 116, 128);
        status.AutoSize = true;
        status.Location = new Point(30, 62);

        display.Text = "00:00:00.000";
        display.Font = new Font("Consolas", 37F, FontStyle.Bold);
        display.ForeColor = Color.FromArgb(38, 91, 145);
        display.TextAlign = ContentAlignment.MiddleCenter;
        display.Location = new Point(22, 112);
        display.Size = new Size(456, 82);
        display.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        startButton.Text = "开始";
        startButton.Location = new Point(28, 222);
        startButton.Size = new Size(444, 48);
        startButton.BackColor = Color.FromArgb(35, 112, 173);
        startButton.ForeColor = Color.White;
        startButton.FlatStyle = FlatStyle.Flat;
        startButton.FlatAppearance.BorderSize = 0;
        startButton.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        startButton.Click += Toggle;

        lapButton.Text = "记圈";
        lapButton.Location = new Point(28, 282);
        lapButton.Size = new Size(215, 42);
        lapButton.Enabled = false;
        lapButton.Click += RecordLap;
        resetButton.Text = "重置";
        resetButton.Location = new Point(257, 282);
        resetButton.Size = new Size(215, 42);
        resetButton.Enabled = false;
        resetButton.Click += ResetTimer;

        var lapsHeading = new Label { Text = "计时记录", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(65, 76, 88), AutoSize = true, Location = new Point(30, 346) };
        lapList.Location = new Point(28, 372);
        lapList.Size = new Size(444, 130);
        lapList.View = View.Details;
        lapList.FullRowSelect = true;
        lapList.GridLines = true;
        lapList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        lapList.Columns.Add("圈数", 90);
        lapList.Columns.Add("分段", 160);
        lapList.Columns.Add("总时间", 170);
        lapList.Font = new Font("Consolas", 10F);

        Controls.AddRange(new Control[] { heading, status, display, startButton, lapButton, resetButton, lapsHeading, lapList });
        ticker.Interval = 8;
        ticker.Tick += delegate { UpdateDisplay(); };
        LoadSavedLaps();
        FormClosing += delegate { SaveData(); };
        UpdateDisplay();
    }

    private void Toggle(object sender, EventArgs e)
    {
        if (running)
        {
            clock.Stop(); running = false; ticker.Stop();
            startButton.Text = "继续";
            status.Text = "毫秒计时  ·  已暂停";
            lapButton.Enabled = false; resetButton.Enabled = true;
        }
        else
        {
            clock.Start(); running = true; ticker.Start();
            startButton.Text = "暂停";
            status.Text = "毫秒计时  ·  正在计时";
            lapButton.Enabled = true; resetButton.Enabled = false;
        }
        UpdateDisplay(); SaveData();
    }

    private void RecordLap(object sender, EventArgs e)
    {
        long now = ElapsedMilliseconds;
        laps.Insert(0, now);
        var item = new ListViewItem("圈 " + laps.Count.ToString("00"));
        item.SubItems.Add(Format(now - lastLap));
        item.SubItems.Add(Format(now));
        lapList.Items.Insert(0, item);
        lastLap = now;
        SaveData();
    }

    private void ResetTimer(object sender, EventArgs e)
    {
        clock.Reset(); elapsedOffset = 0; running = false; ticker.Stop(); laps.Clear(); lastLap = 0;
        lapList.Items.Clear(); startButton.Text = "开始";
        status.Text = "毫秒计时  ·  已就绪";
        lapButton.Enabled = false; resetButton.Enabled = false;
        UpdateDisplay(); SaveData();
    }

    private void UpdateDisplay()
    {
        display.Text = Format(ElapsedMilliseconds);
    }

    private static string Format(long ms)
    {
        long hours = ms / 3600000;
        long minutes = (ms / 60000) % 60;
        long seconds = (ms / 1000) % 60;
        long millis = ms % 1000;
        return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00") + "." + millis.ToString("000");
    }

    public sealed class SavedState
    {
        public long elapsedMilliseconds { get; set; }
        public long[] laps { get; set; }
    }

    private void LoadSavedLaps()
    {
        try
        {
            if (!File.Exists(dataPath)) return;
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var state = serializer.Deserialize<SavedState>(File.ReadAllText(dataPath, Encoding.UTF8));
            if (state == null || state.elapsedMilliseconds < 0) throw new InvalidDataException();
            long[] savedLaps = state.laps ?? new long[0];
            long prior = 0;
            for (int i = savedLaps.Length - 1; i >= 0; i--)
            {
                if (savedLaps[i] < prior || savedLaps[i] > state.elapsedMilliseconds)
                    throw new InvalidDataException();
                prior = savedLaps[i];
            }
            elapsedOffset = state.elapsedMilliseconds;
            laps.AddRange(savedLaps);
            for (int i = 0; i < laps.Count; i++)
            {
                long previous = i + 1 < laps.Count ? laps[i + 1] : 0;
                var item = new ListViewItem("圈 " + (laps.Count - i).ToString("00"));
                item.SubItems.Add(Format(laps[i] - previous));
                item.SubItems.Add(Format(laps[i]));
                lapList.Items.Add(item);
            }
            if (laps.Count > 0) lastLap = laps[0];
            if (elapsedOffset > 0 || laps.Count > 0)
            {
                startButton.Text = "继续";
                resetButton.Enabled = true;
                status.Text = "毫秒计时  ·  已恢复（暂停中）";
            }
        }
        catch { status.Text = "计时记录无法读取，请检查 timer-data.json"; }
    }

    private void SaveData()
    {
        try
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var state = new SavedState { elapsedMilliseconds = ElapsedMilliseconds, laps = laps.ToArray() };
            string tempPath = dataPath + ".tmp";
            File.WriteAllText(tempPath, serializer.Serialize(state), new UTF8Encoding(false));
            if (File.Exists(dataPath)) File.Replace(tempPath, dataPath, null);
            else File.Move(tempPath, dataPath);
        }
        catch (Exception ex) { status.Text = "保存失败: " + ex.Message; }
    }

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TimerWindow());
    }
}
