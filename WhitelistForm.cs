using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace FocusLock
{
    /// <summary>
    /// 智能白名单管理器（三合一）
    /// </summary>
    public class WhitelistForm : Form
    {
        private ListView lstApps;
        private ImageList imgList;
        private Button btnAddInstalled;
        private Button btnAddRunning;
        private Button btnAddBrowse;
        private Button btnRemove;
        private Button btnClose;
        private Label lblTip;

        public WhitelistForm()
        {
            InitializeComponent();
            LoadWhitelist();
        }

        private void InitializeComponent()
        {
            // 窗体设置
            this.Text = "⚙️ 白名单管理";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(560, 520);
            this.Font = new Font("微软雅黑", 10F);
            this.BackColor = Color.FromArgb(247, 248, 252);

            // 图标列表
            imgList = new ImageList();
            imgList.ImageSize = new Size(32, 32);
            imgList.ColorDepth = ColorDepth.Depth32Bit;

            // 标题
            Label lblTitle = new Label();
            lblTitle.Text = "📋 允许使用的软件";
            lblTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(51, 51, 51);
            lblTitle.Location = new Point(20, 15);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // 列表
            lstApps = new ListView();
            lstApps.View = View.Details;
            lstApps.FullRowSelect = true;
            lstApps.GridLines = false;
            lstApps.Location = new Point(20, 45);
            lstApps.Size = new Size(505, 320);
            lstApps.SmallImageList = imgList;
            lstApps.BackColor = Color.White;
            lstApps.BorderStyle = BorderStyle.FixedSingle;
            lstApps.Font = new Font("微软雅黑", 10F);
            lstApps.Columns.Add("软件名称", 280);
            lstApps.Columns.Add("进程名", 180);
            this.Controls.Add(lstApps);

            // 添加方式按钮组
            GroupBox grpAdd = new GroupBox();
            grpAdd.Text = "➕ 添加软件（三种方式任选）";
            grpAdd.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            grpAdd.ForeColor = Color.FromArgb(51, 51, 51);
            grpAdd.Location = new Point(20, 375);
            grpAdd.Size = new Size(505, 60);
            grpAdd.BackColor = Color.White;
            this.Controls.Add(grpAdd);

            btnAddInstalled = new Button();
            btnAddInstalled.Text = "📦 从已安装软件";
            btnAddInstalled.Font = new Font("微软雅黑", 9F);
            btnAddInstalled.Location = new Point(15, 22);
            btnAddInstalled.Size = new Size(150, 30);
            btnAddInstalled.FlatStyle = FlatStyle.Flat;
            btnAddInstalled.ForeColor = Color.FromArgb(102, 126, 234);
            btnAddInstalled.BackColor = Color.FromArgb(240, 243, 255);
            btnAddInstalled.Cursor = Cursors.Hand;
            btnAddInstalled.Click += btnAddInstalled_Click;
            grpAdd.Controls.Add(btnAddInstalled);

            btnAddRunning = new Button();
            btnAddRunning.Text = "🔄 从运行中程序";
            btnAddRunning.Font = new Font("微软雅黑", 9F);
            btnAddRunning.Location = new Point(173, 22);
            btnAddRunning.Size = new Size(150, 30);
            btnAddRunning.FlatStyle = FlatStyle.Flat;
            btnAddRunning.ForeColor = Color.FromArgb(100, 180, 100);
            btnAddRunning.BackColor = Color.FromArgb(240, 250, 240);
            btnAddRunning.Cursor = Cursors.Hand;
            btnAddRunning.Click += btnAddRunning_Click;
            grpAdd.Controls.Add(btnAddRunning);

            btnAddBrowse = new Button();
            btnAddBrowse.Text = "📁 浏览文件选择";
            btnAddBrowse.Font = new Font("微软雅黑", 9F);
            btnAddBrowse.Location = new Point(331, 22);
            btnAddBrowse.Size = new Size(150, 30);
            btnAddBrowse.FlatStyle = FlatStyle.Flat;
            btnAddBrowse.ForeColor = Color.FromArgb(230, 150, 50);
            btnAddBrowse.BackColor = Color.FromArgb(255, 250, 240);
            btnAddBrowse.Cursor = Cursors.Hand;
            btnAddBrowse.Click += btnAddBrowse_Click;
            grpAdd.Controls.Add(btnAddBrowse);

            // 删除按钮
            btnRemove = new Button();
            btnRemove.Text = "🗑️ 删除选中";
            btnRemove.Font = new Font("微软雅黑", 10F);
            btnRemove.Location = new Point(20, 445);
            btnRemove.Size = new Size(120, 30);
            btnRemove.FlatStyle = FlatStyle.Flat;
            btnRemove.ForeColor = Color.FromArgb(220, 80, 80);
            btnRemove.BackColor = Color.FromArgb(255, 240, 240);
            btnRemove.Cursor = Cursors.Hand;
            btnRemove.Click += btnRemove_Click;
            this.Controls.Add(btnRemove);

            // 关闭按钮
            btnClose = new Button();
            btnClose.Text = "完成";
            btnClose.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            btnClose.Location = new Point(445, 445);
            btnClose.Size = new Size(80, 30);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.ForeColor = Color.White;
            btnClose.BackColor = Color.FromArgb(102, 126, 234);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            // 提示
            lblTip = new Label();
            lblTip.Text = "💡 专注模式下，只有白名单里的软件可以使用";
            lblTip.Font = new Font("微软雅黑", 9F);
            lblTip.ForeColor = Color.FromArgb(153, 153, 153);
            lblTip.Location = new Point(150, 450);
            lblTip.AutoSize = true;
            this.Controls.Add(lblTip);
        }

        /// <summary>
        /// 加载白名单到列表
        /// </summary>
        private void LoadWhitelist()
        {
            lstApps.Items.Clear();
            imgList.Images.Clear();

            List<WhitelistItem> items = ConfigHelper.GetWhitelist();
            int iconIndex = 0;

            foreach (WhitelistItem item in items)
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
                    // 默认图标
                    imgList.Images.Add(SystemIcons.Application);
                }

                // 添加列表项
                ListViewItem lvi = new ListViewItem(item.DisplayName);
                lvi.ImageIndex = iconIndex;
                lvi.SubItems.Add(item.ProcessName);
                lvi.Tag = item.ProcessName;
                lstApps.Items.Add(lvi);

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
        /// 从已安装软件添加
        /// </summary>
        private void btnAddInstalled_Click(object sender, EventArgs e)
        {
            using (AddAppDialog dlg = new AddAppDialog(AddAppMode.Installed))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadWhitelist();
                }
            }
        }

        /// <summary>
        /// 从运行中程序添加
        /// </summary>
        private void btnAddRunning_Click(object sender, EventArgs e)
        {
            using (AddAppDialog dlg = new AddAppDialog(AddAppMode.Running))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadWhitelist();
                }
            }
        }

        /// <summary>
        /// 浏览文件添加
        /// </summary>
        private void btnAddBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "应用程序 (*.exe)|*.exe|所有文件 (*.*)|*.*";
            ofd.Title = "选择要添加的软件";
            ofd.CheckFileExists = true;

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                AppInfo app = AppScanner.GetAppFromExe(ofd.FileName);
                if (app != null && !string.IsNullOrEmpty(app.ProcessName))
                {
                    if (ConfigHelper.IsInWhitelist(app.ProcessName))
                    {
                        MessageBox.Show(this, "这个软件已经在白名单里啦~", "提示",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        ConfigHelper.AddToWhitelist(app);
                        LoadWhitelist();
                        MessageBox.Show(this, string.Format("已添加「{0}」到白名单！", app.DisplayName),
                            "添加成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        /// <summary>
        /// 删除选中
        /// </summary>
        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lstApps.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "请先选择要删除的软件~", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (ListViewItem item in lstApps.SelectedItems)
            {
                string procName = item.Tag as string;
                if (procName != null && procName.ToLower() == "focuslock")
                {
                    MessageBox.Show(this, "不能删除我们自己哦！", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }
                if (procName != null)
                {
                    ConfigHelper.RemoveFromWhitelist(procName);
                }
            }

            LoadWhitelist();
        }
    }

    /// <summary>
    /// 添加软件的模式
    /// </summary>
    public enum AddAppMode
    {
        Installed,
        Running
    }

    /// <summary>
    /// 添加软件的选择对话框
    /// </summary>
    public class AddAppDialog : Form
    {
        private ListView lstApps;
        private ImageList imgList;
        private Button btnOk;
        private Button btnCancel;
        private Button btnRefresh;
        private Label lblStatus;
        private AddAppMode _mode;

        public AddAppDialog(AddAppMode mode)
        {
            _mode = mode;
            InitializeComponent();
            StartScan();
        }

        private void InitializeComponent()
        {
            this.Text = _mode == AddAppMode.Installed ? "📦 从已安装软件添加" : "🔄 从运行中程序添加";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(500, 480);
            this.Font = new Font("微软雅黑", 10F);
            this.BackColor = Color.FromArgb(247, 248, 252);

            imgList = new ImageList();
            imgList.ImageSize = new Size(32, 32);
            imgList.ColorDepth = ColorDepth.Depth32Bit;

            Label lblTip = new Label();
            lblTip.Text = "勾选要添加到白名单的软件：";
            lblTip.Font = new Font("微软雅黑", 11F);
            lblTip.ForeColor = Color.FromArgb(51, 51, 51);
            lblTip.Location = new Point(15, 12);
            lblTip.AutoSize = true;
            this.Controls.Add(lblTip);

            btnRefresh = new Button();
            btnRefresh.Text = "🔄 刷新";
            btnRefresh.Font = new Font("微软雅黑", 9F);
            btnRefresh.Location = new Point(390, 8);
            btnRefresh.Size = new Size(75, 25);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.ForeColor = Color.FromArgb(102, 126, 234);
            btnRefresh.BackColor = Color.White;
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += (s, e) => StartScan();
            this.Controls.Add(btnRefresh);

            lstApps = new ListView();
            lstApps.View = View.Details;
            lstApps.CheckBoxes = true;
            lstApps.FullRowSelect = true;
            lstApps.Location = new Point(15, 40);
            lstApps.Size = new Size(455, 355);
            lstApps.SmallImageList = imgList;
            lstApps.BackColor = Color.White;
            lstApps.BorderStyle = BorderStyle.FixedSingle;
            lstApps.Columns.Add("软件名称", 300);
            lstApps.Columns.Add("进程名", 120);
            this.Controls.Add(lstApps);

            lblStatus = new Label();
            lblStatus.Text = "";
            lblStatus.Font = new Font("微软雅黑", 9F);
            lblStatus.ForeColor = Color.FromArgb(153, 153, 153);
            lblStatus.Location = new Point(15, 400);
            lblStatus.AutoSize = true;
            this.Controls.Add(lblStatus);

            btnOk = new Button();
            btnOk.Text = "添加选中";
            btnOk.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            btnOk.Location = new Point(305, 420);
            btnOk.Size = new Size(80, 30);
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.ForeColor = Color.White;
            btnOk.BackColor = Color.FromArgb(100, 180, 100);
            btnOk.Cursor = Cursors.Hand;
            btnOk.Click += btnOk_Click;
            this.Controls.Add(btnOk);

            btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Font = new Font("微软雅黑", 10F);
            btnCancel.Location = new Point(395, 420);
            btnCancel.Size = new Size(75, 30);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.ForeColor = Color.FromArgb(102, 102, 102);
            btnCancel.BackColor = Color.White;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);
        }

        /// <summary>
        /// 开始扫描
        /// </summary>
        private void StartScan()
        {
            lstApps.Items.Clear();
            imgList.Images.Clear();
            lblStatus.Text = "正在扫描中...";
            btnRefresh.Enabled = false;
            btnOk.Enabled = false;

            Timer scanTimer = new Timer();
            scanTimer.Interval = 50;
            scanTimer.Tick += (s, e) =>
            {
                scanTimer.Stop();

                List<AppInfo> apps = new List<AppInfo>();
                if (_mode == AddAppMode.Installed)
                {
                    apps = AppScanner.ScanInstalledApps();
                }
                else
                {
                    apps = AppScanner.ScanRunningApps();
                }

                int iconIdx = 0;
                int skipped = 0;
                foreach (AppInfo app in apps)
                {
                    if (ConfigHelper.IsInWhitelist(app.ProcessName))
                    {
                        skipped++;
                        continue;
                    }

                    if (app.AppIcon != null)
                    {
                        imgList.Images.Add(app.AppIcon);
                    }
                    else
                    {
                        imgList.Images.Add(SystemIcons.Application);
                    }

                    ListViewItem lvi = new ListViewItem(app.DisplayName);
                    lvi.ImageIndex = iconIdx;
                    lvi.SubItems.Add(app.ProcessName);
                    lvi.Tag = app;
                    lstApps.Items.Add(lvi);
                    iconIdx++;
                }

                lblStatus.Text = string.Format("共找到 {0} 个软件，已跳过 {1} 个已添加的",
                    apps.Count, skipped);
                btnRefresh.Enabled = true;
                btnOk.Enabled = true;
            };
            scanTimer.Start();
        }

        /// <summary>
        /// 确定添加
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            int added = 0;
            foreach (ListViewItem item in lstApps.CheckedItems)
            {
                AppInfo app = item.Tag as AppInfo;
                if (app != null && !ConfigHelper.IsInWhitelist(app.ProcessName))
                {
                    ConfigHelper.AddToWhitelist(app);
                    added++;
                }
            }

            if (added == 0)
            {
                MessageBox.Show(this, "请先勾选要添加的软件~", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show(this, string.Format("成功添加了 {0} 个软件！", added),
                "添加成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
