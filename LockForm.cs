using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FocusLock
{
    /// <summary>
    /// 锁定界面 - 全屏遮罩 + 倒计时 + 进程监控
    /// </summary>
    public class LockForm : Form
    {
        // 控件
        private Label lblTitle;
        private Label lblTimer;
        private Label lblEncourage;
        private Label lblTip;
        private Label lblKilled;
        private ProgressBar progressBar;

        // 总时长（秒）
        private int _totalSeconds;
        // 剩余秒数
        private int _remainingSeconds;
        // 倒计时定时器
        private Timer _countdownTimer;

        // 进程管理器
        private ProcessManager _processManager;

        // 键盘钩子
        private KeyboardHook _keyboardHook;

        // 原来的任务管理器状态（用于恢复）
        private bool _taskMgrWasDisabled = false;

        // 是否已解锁
        private bool _unlocked = false;

        // 是否是继续模式（从上次未完成的地方继续）
        private bool _isContinueMode = false;

        /// <summary>
        /// 构造函数（普通模式：从头开始）
        /// </summary>
        /// <param name="minutes">专注时长（分钟）</param>
        public LockForm(int minutes)
        {
            _totalSeconds = minutes * 60;
            _remainingSeconds = _totalSeconds;
            _isContinueMode = false;

            InitializeComponent();
            UpdateTimerDisplay();
        }

        /// <summary>
        /// 构造函数（继续模式：从上次未完成的地方继续）
        /// </summary>
        /// <param name="remainingSeconds">剩余秒数</param>
        /// <param name="isContinue">传true表示继续模式（用于区分普通构造函数）</param>
        public LockForm(int remainingSeconds, bool isContinue)
        {
            _totalSeconds = remainingSeconds; // 进度条从0开始，剩余多少就显示多少
            _remainingSeconds = remainingSeconds;
            _isContinueMode = true;

            InitializeComponent();
            UpdateTimerDisplay();
        }

        /// <summary>
        /// 初始化界面
        /// </summary>
        private void InitializeComponent()
        {
            // 窗体设置 - 全屏无边框
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(26, 26, 46); // 深色背景
            this.Opacity = 0.95;
            this.ShowInTaskbar = false;

            // 标题
            lblTitle = new Label();
            lblTitle.Text = "🔒 专注模式进行中...";
            lblTitle.Font = new Font("微软雅黑", 28F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.AutoSize = true;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            // 倒计时
            lblTimer = new Label();
            lblTimer.Text = "00:00:00";
            lblTimer.Font = new Font("Consolas", 80F, FontStyle.Bold);
            lblTimer.ForeColor = Color.FromArgb(102, 126, 234);
            lblTimer.AutoSize = true;
            lblTimer.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTimer);

            // 进度条
            progressBar = new ProgressBar();
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.ForeColor = Color.FromArgb(102, 126, 234);
            progressBar.BackColor = Color.FromArgb(50, 50, 80);
            this.Controls.Add(progressBar);

            // 鼓励文字
            lblEncourage = new Label();
            lblEncourage.Text = "加油！专心背稿子，你可以的！";
            lblEncourage.Font = new Font("微软雅黑", 16F);
            lblEncourage.ForeColor = Color.FromArgb(200, 200, 220);
            lblEncourage.AutoSize = true;
            lblEncourage.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblEncourage);

            // 提示
            lblTip = new Label();
            lblTip.Text = "💡 只有白名单里的软件可以使用哦 ~";
            lblTip.Font = new Font("微软雅黑", 12F);
            lblTip.ForeColor = Color.FromArgb(150, 150, 180);
            lblTip.AutoSize = true;
            lblTip.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTip);

            // 已关闭进程数
            lblKilled = new Label();
            lblKilled.Text = "🛡️ 已为您拦截 0 个分心软件";
            lblKilled.Font = new Font("微软雅黑", 11F);
            lblKilled.ForeColor = Color.FromArgb(120, 200, 120);
            lblKilled.AutoSize = true;
            lblKilled.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblKilled);

            // 调整控件位置（居中）
            this.Resize += LockForm_Resize;
            this.Load += LockForm_Load;
            this.FormClosing += LockForm_FormClosing;
        }

        /// <summary>
        /// 窗体加载时
        /// </summary>
        private void LockForm_Load(object sender, EventArgs e)
        {
            // 居中所有控件
            CenterControls();

            // 禁用任务管理器
            DisableTaskManager();

            // 启动进程监控
            _processManager = new ProcessManager();
            _processManager.Start();

            // 启动键盘钩子
            _keyboardHook = new KeyboardHook();
            _keyboardHook.EmergencyUnlock += KeyboardHook_EmergencyUnlock;
            _keyboardHook.Start();

            // 启动倒计时
            _countdownTimer = new Timer();
            _countdownTimer.Interval = 1000; // 1秒
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            // 更新被杀进程数的定时器
            Timer updateKilledTimer = new Timer();
            updateKilledTimer.Interval = 2000;
            updateKilledTimer.Tick += (s, ev) =>
            {
                if (_processManager != null)
                {
                    lblKilled.Text = string.Format("🛡️ 已为您拦截 {0} 个分心软件", _processManager.KilledCount);
                }
            };
            updateKilledTimer.Start();
        }

        /// <summary>
        /// 窗体大小改变时，重新居中
        /// </summary>
        private void LockForm_Resize(object sender, EventArgs e)
        {
            CenterControls();
        }

        /// <summary>
        /// 居中所有控件
        /// </summary>
        private void CenterControls()
        {
            int centerX = this.ClientSize.Width / 2;
            int centerY = this.ClientSize.Height / 2;

            // 标题
            lblTitle.Left = centerX - lblTitle.Width / 2;
            lblTitle.Top = centerY - 220;

            // 倒计时
            lblTimer.Left = centerX - lblTimer.Width / 2;
            lblTimer.Top = centerY - 140;

            // 进度条
            progressBar.Width = 500;
            progressBar.Height = 10;
            progressBar.Left = centerX - 250;
            progressBar.Top = centerY - 20;

            // 鼓励文字
            lblEncourage.Left = centerX - lblEncourage.Width / 2;
            lblEncourage.Top = centerY + 20;

            // 提示
            lblTip.Left = centerX - lblTip.Width / 2;
            lblTip.Top = centerY + 70;

            // 已拦截数
            lblKilled.Left = centerX - lblKilled.Width / 2;
            lblKilled.Top = centerY + 120;
        }

        /// <summary>
        /// 倒计时每秒触发
        /// </summary>
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _remainingSeconds--;

            UpdateTimerDisplay();
            UpdateProgressBar();

            if (_remainingSeconds <= 0)
            {
                // 时间到！
                _countdownTimer.Stop();
                FinishFocus();
            }
            else if (_remainingSeconds <= 60)
            {
                // 最后一分钟，红色提醒
                lblTimer.ForeColor = Color.FromArgb(255, 100, 100);
            }
        }

        /// <summary>
        /// 更新倒计时显示
        /// </summary>
        private void UpdateTimerDisplay()
        {
            int hours = _remainingSeconds / 3600;
            int minutes = (_remainingSeconds % 3600) / 60;
            int seconds = _remainingSeconds % 60;

            if (hours > 0)
            {
                lblTimer.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
            }
            else
            {
                lblTimer.Text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
            }
        }

        /// <summary>
        /// 更新进度条
        /// </summary>
        private void UpdateProgressBar()
        {
            if (_totalSeconds > 0)
            {
                double progress = (double)(_totalSeconds - _remainingSeconds) / _totalSeconds * 100;
                progressBar.Value = (int)Math.Min(100, Math.Max(0, progress));
            }
        }

        /// <summary>
        /// 完成专注
        /// </summary>
        private void FinishFocus()
        {
            _unlocked = true;

            // 清除未完成记录（正常完成了就不需要继续了）
            ConfigHelper.ClearUnfinishedFocus();

            // 播放提示音
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch { }

            // 显示恭喜
            lblTitle.Text = "🎉 恭喜完成专注！";
            lblTimer.ForeColor = Color.FromArgb(100, 220, 100);
            lblEncourage.Text = "太棒了！你坚持下来了！";

            // 3秒后自动关闭
            Timer closeTimer = new Timer();
            closeTimer.Interval = 3000;
            closeTimer.Tick += (s, ev) =>
            {
                closeTimer.Stop();
                this.Close();
            };
            closeTimer.Start();
        }

        /// <summary>
        /// 紧急解锁
        /// </summary>
        private void KeyboardHook_EmergencyUnlock(object sender, EventArgs e)
        {
            // 用Invoke确保在UI线程执行
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => EmergencyUnlock()));
            }
            else
            {
                EmergencyUnlock();
            }
        }

        /// <summary>
        /// 紧急解锁处理
        /// </summary>
        private void EmergencyUnlock()
        {
            if (_unlocked) return;

            // 询问是否真的要紧急解锁
            DialogResult result = MessageBox.Show(
                "检测到紧急解锁请求！\n\n确定要提前结束专注模式吗？\n\n（连续按5次ESC触发紧急解锁）",
                "⚠️ 紧急解锁",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _unlocked = true;

                // 保存剩余时间，方便误触后继续
                ConfigHelper.SaveUnfinishedFocus(_remainingSeconds);

                MessageBox.Show("已紧急解锁！\n剩余时间已保存，下次可以选择「继续上次专注」哦~ 💪", 
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }

        /// <summary>
        /// 窗体关闭时，恢复所有设置
        /// </summary>
        private void LockForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 如果没解锁，不让关
            if (!_unlocked && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }

            // 停止定时器
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Dispose();
            }

            // 停止进程监控
            if (_processManager != null)
            {
                _processManager.Dispose();
                _processManager = null;
            }

            // 停止键盘钩子
            if (_keyboardHook != null)
            {
                _keyboardHook.Dispose();
                _keyboardHook = null;
            }

            // 恢复任务管理器
            EnableTaskManager();
        }

        #region 任务管理器禁用/恢复

        /// <summary>
        /// 禁用任务管理器
        /// </summary>
        private void DisableTaskManager()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);

                if (key == null)
                {
                    key = Registry.CurrentUser.CreateSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                }

                // 记录原来的值
                object oldValue = key.GetValue("DisableTaskMgr");
                _taskMgrWasDisabled = (oldValue != null && oldValue.ToString() == "1");

                // 设置禁用
                key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                key.Close();
            }
            catch (Exception ex)
            {
                // 失败也没关系，不影响主要功能
            }
        }

        /// <summary>
        /// 恢复任务管理器
        /// </summary>
        private void EnableTaskManager()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);

                if (key != null)
                {
                    if (_taskMgrWasDisabled)
                    {
                        // 原来是禁用的，保持禁用
                        key.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                    }
                    else
                    {
                        // 原来是启用的，删掉这个值（恢复默认）
                        key.DeleteValue("DisableTaskMgr", false);
                    }
                    key.Close();
                }
            }
            catch (Exception ex)
            {
                // 失败就算了
            }
        }

        #endregion
    }
}
