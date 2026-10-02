using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FocusLock
{
    /// <summary>
    /// 主界面 - 设置专注时间、管理白名单
    /// </summary>
    public class MainForm : Form
    {
        // 控件
        private NumericUpDown numMinutes;
        private Button btnStart;
        private Button btnContinue;       // 继续上次专注按钮
        private Button btnManageWhitelist;
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblTip;
        private GroupBox grpWhitelist;
        private ListView lstWhitelist;
        private ImageList imgList;

        // 预设时间按钮
        private Button btn15min;
        private Button btn30min;
        private Button btn60min;
        private Button btn90min;

        public MainForm()
        {
            InitializeComponent();
            LoadWhitelistToList();
            RefreshContinueButton();

            // 每次窗体激活时刷新（从锁定界面回来后更新状态）
            this.Activated += (sender, e) => {
                RefreshContinueButton();
                LoadWhitelistToList();
            };
        }

        /// <summary>
        /// 初始化界面
        /// </summary>
        private void InitializeComponent()
        {
            // 窗体设置
            this.Text = "🔒 专注背稿锁机";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(520, 640);
            this.BackColor = Color.FromArgb(247, 248, 252);
            this.Font = new Font("微软雅黑", 10F);

            // 图标列表
            imgList = new ImageList();
            imgList.ImageSize = new Size(24, 24);
            imgList.ColorDepth = ColorDepth.Depth32Bit;

            // 标题
            lblTitle = new Label();
            lblTitle.Text = "📝 专注背稿锁机";
            lblTitle.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(102, 126, 234);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(30, 25);
            this.Controls.Add(lblTitle);

            // 副标题
            lblSubtitle = new Label();
            lblSubtitle.Text = "把 distractions 都锁起来，专心背稿子！";
            lblSubtitle.Font = new Font("微软雅黑", 10F);
            lblSubtitle.ForeColor = Color.FromArgb(136, 136, 136);
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(32, 65);
            this.Controls.Add(lblSubtitle);

            // 时间设置区域
            Label lblTimeTitle = new Label();
            lblTimeTitle.Text = "⏱️ 设置专注时长";
            lblTimeTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            lblTimeTitle.ForeColor = Color.FromArgb(51, 51, 51);
            lblTimeTitle.AutoSize = true;
            lblTimeTitle.Location = new Point(30, 105);
            this.Controls.Add(lblTimeTitle);

            // 分钟输入框
            numMinutes = new NumericUpDown();
            numMinutes.Minimum = 1;
            numMinutes.Maximum = 480; // 最多8小时
            numMinutes.Value = ConfigHelper.DefaultMinutes;
            numMinutes.Font = new Font("微软雅黑", 24F, FontStyle.Bold);
            numMinutes.ForeColor = Color.FromArgb(102, 126, 234);
            numMinutes.TextAlign = HorizontalAlignment.Center;
            numMinutes.Size = new Size(120, 50);
            numMinutes.Location = new Point(180, 135);
            numMinutes.BorderStyle = BorderStyle.FixedSingle;
            numMinutes.BackColor = Color.White;
            this.Controls.Add(numMinutes);

            Label lblMinUnit = new Label();
            lblMinUnit.Text = "分钟";
            lblMinUnit.Font = new Font("微软雅黑", 12F);
            lblMinUnit.ForeColor = Color.FromArgb(102, 102, 102);
            lblMinUnit.AutoSize = true;
            lblMinUnit.Location = new Point(310, 155);
            this.Controls.Add(lblMinUnit);

            // 快速选择按钮
            btn15min = CreateQuickBtn("15分钟", 30, 200, 15);
            btn30min = CreateQuickBtn("30分钟", 125, 200, 30);
            btn60min = CreateQuickBtn("1小时", 220, 200, 60);
            btn90min = CreateQuickBtn("1.5小时", 315, 200, 90);

            this.Controls.Add(btn15min);
            this.Controls.Add(btn30min);
            this.Controls.Add(btn60min);
            this.Controls.Add(btn90min);

            // 白名单区域
            grpWhitelist = new GroupBox();
            grpWhitelist.Text = "✅ 可用软件（白名单）";
            grpWhitelist.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            grpWhitelist.ForeColor = Color.FromArgb(51, 51, 51);
            grpWhitelist.Location = new Point(30, 260);
            grpWhitelist.Size = new Size(440, 200);
            grpWhitelist.BackColor = Color.White;
            this.Controls.Add(grpWhitelist);

            lstWhitelist = new ListView();
            lstWhitelist.View = View.Details;
            lstWhitelist.FullRowSelect = true;
            lstWhitelist.HeaderStyle = ColumnHeaderStyle.None;
            lstWhitelist.SmallImageList = imgList;
            lstWhitelist.Location = new Point(15, 25);
            lstWhitelist.Size = new Size(410, 140);
            lstWhitelist.BorderStyle = BorderStyle.FixedSingle;
            lstWhitelist.BackColor = Color.FromArgb(247, 248, 252);
            lstWhitelist.Columns.Add("软件名", 380);
            grpWhitelist.Controls.Add(lstWhitelist);

            btnManageWhitelist = new Button();
            btnManageWhitelist.Text = "⚙️ 管理白名单";
            btnManageWhitelist.Font = new Font("微软雅黑", 9F);
            btnManageWhitelist.Location = new Point(305, 170);
            btnManageWhitelist.Size = new Size(120, 25);
            btnManageWhitelist.FlatStyle = FlatStyle.Flat;
            btnManageWhitelist.ForeColor = Color.FromArgb(102, 126, 234);
            btnManageWhitelist.BackColor = Color.White;
            btnManageWhitelist.Cursor = Cursors.Hand;
            btnManageWhitelist.Click += btnManageWhitelist_Click;
            grpWhitelist.Controls.Add(btnManageWhitelist);

            // 继续上次专注按钮
            btnContinue = new Button();
            btnContinue.Text = "▶️ 继续上次专注";
            btnContinue.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            btnContinue.ForeColor = Color.White;
            btnContinue.BackColor = Color.FromArgb(100, 180, 100);
            btnContinue.FlatStyle = FlatStyle.Flat;
            btnContinue.FlatAppearance.BorderSize = 0;
            btnContinue.Size = new Size(440, 45);
            btnContinue.Location = new Point(30, 470);
            btnContinue.Cursor = Cursors.Hand;
            btnContinue.Click += btnContinue_Click;
            btnContinue.Visible = false;
            this.Controls.Add(btnContinue);

            // 开始按钮
            btnStart = new Button();
            btnStart.Text = "🚀 开始新的专注";
            btnStart.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            btnStart.ForeColor = Color.White;
            btnStart.BackColor = Color.FromArgb(102, 126, 234);
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Size = new Size(440, 55);
            btnStart.Location = new Point(30, 525);
            btnStart.Cursor = Cursors.Hand;
            btnStart.Click += btnStart_Click;
            this.Controls.Add(btnStart);

            // 提示文字
            lblTip = new Label();
            lblTip.Text = "💡 小提示：专注模式下只有白名单里的软件能用哦~";
            lblTip.Font = new Font("微软雅黑", 9F);
            lblTip.ForeColor = Color.FromArgb(153, 153, 153);
            lblTip.AutoSize = true;
            lblTip.Location = new Point(30, 595);
            this.Controls.Add(lblTip);
        }

        /// <summary>
        /// 创建快速选择按钮
        /// </summary>
        private Button CreateQuickBtn(string text, int x, int y, int minutes)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("微软雅黑", 9F);
            btn.Location = new Point(x, y);
            btn.Size = new Size(85, 30);
            btn.FlatStyle = FlatStyle.Flat;
            btn.ForeColor = Color.FromArgb(102, 126, 234);
            btn.BackColor = Color.White;
            btn.Cursor = Cursors.Hand;
            btn.Click += (sender, e) => { numMinutes.Value = minutes; };
            return btn;
        }

        /// <summary>
        /// 加载白名单到列表（带图标）
        /// </summary>
        private void LoadWhitelistToList()
        {
            lstWhitelist.Items.Clear();
            imgList.Images.Clear();

            List<WhitelistItem> whitelist = ConfigHelper.GetWhitelist();
            int iconIndex = 0;

            foreach (WhitelistItem item in whitelist)
            {
                // 跳过系统进程，只显示用户关心的
                if (IsSystemProcess(item.ProcessName))
                    continue;

                // 添加图标
                Icon icon = item.GetIcon();
                if (icon != null)
                {
                    imgList.Images.Add(icon);
                }
                else
                {
                    imgList.Images.Add(SystemIcons.Application);
                }

                ListViewItem lvi = new ListViewItem(item.DisplayName);
                lvi.ImageIndex = iconIndex;
                lstWhitelist.Items.Add(lvi);

                iconIndex++;
            }
        }

        /// <summary>
        /// 判断是不是系统进程
        /// </summary>
        private bool IsSystemProcess(string name)
        {
            string[] systemProcs = {
                "explorer", "svchost", "winlogon", "csrss", "smss",
                "services", "lsass", "dwm", "conhost", "searchhost",
                "startmenuexperiencehost", "textinputhost", "runtimebroker",
                "shellexperiencehost", "windowsterminal", "taskhostw",
                "ctfmon", "audiodg", "fontdrvhost", "nvcontainer",
                "nvvsvc", "nvxdsync", "sihost",
                "securityhealthservice", "securityhealthsystray",
                "msmpeng", "nissrv"
            };
            return Array.Exists(systemProcs, p => p == name.ToLower());
        }

        /// <summary>
        /// 刷新继续按钮的显示状态
        /// </summary>
        private void RefreshContinueButton()
        {
            if (ConfigHelper.HasUnfinishedFocus)
            {
                btnContinue.Visible = true;
                int secs = ConfigHelper.RemainingSeconds;
                int min = secs / 60;
                int s = secs % 60;
                if (min > 0)
                {
                    btnContinue.Text = string.Format("▶️ 继续上次专注（还剩 {0}分{1}秒）", min, s);
                }
                else
                {
                    btnContinue.Text = string.Format("▶️ 继续上次专注（还剩 {0}秒）", s);
                }
            }
            else
            {
                btnContinue.Visible = false;
            }
        }

        /// <summary>
        /// 继续上次专注按钮点击
        /// </summary>
        private void btnContinue_Click(object sender, EventArgs e)
        {
            if (!ConfigHelper.HasUnfinishedFocus) return;

            int remaining = ConfigHelper.RemainingSeconds;

            // 确认对话框
            DialogResult result = MessageBox.Show(
                string.Format("确定要继续上次的专注吗？\n\n还剩 {0} 分 {1} 秒", remaining / 60, remaining % 60),
                "继续专注",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // 3秒倒计时提示
                using (CountdownForm countdown = new CountdownForm(3))
                {
                    countdown.ShowDialog();
                }

                // 打开锁定界面（继续模式）
                this.Hide();
                using (LockForm lockForm = new LockForm(remaining, true))
                {
                    lockForm.ShowDialog();
                }
                this.Show();
            }
        }

        /// <summary>
        /// 开始专注按钮点击
        /// </summary>
        private void btnStart_Click(object sender, EventArgs e)
        {
            int minutes = (int)numMinutes.Value;

            // 保存默认时长
            ConfigHelper.DefaultMinutes = minutes;

            // 如果有未完成的，问用户是继续还是重新开始
            if (ConfigHelper.HasUnfinishedFocus)
            {
                DialogResult choose = MessageBox.Show(
                    "你上次有未完成的专注，是要继续还是重新开始？\n\n" +
                    "是 = 继续上次的进度\n" +
                    "否 = 重新开始新的专注",
                    "选择模式",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (choose == DialogResult.Cancel) return;

                if (choose == DialogResult.Yes)
                {
                    // 继续
                    btnContinue_Click(sender, e);
                    return;
                }
                // 否的话继续往下，重新开始
            }

            // 确认对话框
            DialogResult result = MessageBox.Show(
                string.Format("确定要开始 {0} 分钟的专注背稿吗？\n\n开始后，只有白名单里的软件能用，时间不到不能解锁哦！", minutes),
                "确认开始专注",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // 清除之前的未完成记录（因为重新开始了）
                ConfigHelper.ClearUnfinishedFocus();

                // 3秒倒计时提示
                using (CountdownForm countdown = new CountdownForm(3))
                {
                    countdown.ShowDialog();
                }

                // 打开锁定界面
                this.Hide();
                using (LockForm lockForm = new LockForm(minutes))
                {
                    lockForm.ShowDialog();
                }
                this.Show();
            }
        }

        /// <summary>
        /// 管理白名单按钮点击
        /// </summary>
        private void btnManageWhitelist_Click(object sender, EventArgs e)
        {
            using (WhitelistForm wlForm = new WhitelistForm())
            {
                wlForm.ShowDialog();
                LoadWhitelistToList();
            }
        }
    }

    /// <summary>
    /// 倒计时提示窗体（3-2-1 开始）
    /// </summary>
    public class CountdownForm : Form
    {
        private Label lblCountdown;
        private Label lblTip;
        private Timer _timer;
        private int _count;

        public CountdownForm(int seconds)
        {
            _count = seconds;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 300);
            this.BackColor = Color.FromArgb(102, 126, 234);
            this.TopMost = true;
            this.Opacity = 0.95;

            lblCountdown = new Label();
            lblCountdown.Text = _count.ToString();
            lblCountdown.Font = new Font("微软雅黑", 100F, FontStyle.Bold);
            lblCountdown.ForeColor = Color.White;
            lblCountdown.AutoSize = true;
            lblCountdown.Location = new Point(150, 70);
            this.Controls.Add(lblCountdown);

            lblTip = new Label();
            lblTip.Text = "准备好，马上进入专注模式...";
            lblTip.Font = new Font("微软雅黑", 14F);
            lblTip.ForeColor = Color.White;
            lblTip.AutoSize = true;
            lblTip.Location = new Point(80, 210);
            this.Controls.Add(lblTip);

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _count--;
            if (_count > 0)
            {
                lblCountdown.Text = _count.ToString();
            }
            else
            {
                _timer.Stop();
                this.Close();
            }
        }
    }
}
