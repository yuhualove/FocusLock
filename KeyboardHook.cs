using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FocusLock
{
    /// <summary>
    /// 键盘钩子 - 拦截危险快捷键，防止用户退出专注模式
    /// </summary>
    public class KeyboardHook : IDisposable
    {
        // Windows API 常量
        private const int WH_KEYBOARD_LL = 13; // 低级键盘钩子
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        // 虚拟键码
        private const int VK_LWIN = 0x5B;  // 左Win键
        private const int VK_RWIN = 0x5C;  // 右Win键
        private const int VK_TAB = 0x09;   // Tab键
        private const int VK_ESCAPE = 0x1B; // ESC键
        private const int VK_CONTROL = 0x11; // Ctrl键
        private const int VK_MENU = 0x12;    // Alt键
        private const int VK_DELETE = 0x2E;  // Delete键

        // 钩子委托
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // 钩子句柄
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;

        // 是否启用钩子
        private bool _enabled = false;

        // ESC连续按下计数（用于紧急解锁）
        private int _escCount = 0;
        private DateTime _lastEscTime = DateTime.MinValue;

        // 紧急解锁事件
        public event EventHandler EmergencyUnlock;

        // 紧急解锁需要按几次ESC
        private const int EMERGENCY_ESC_COUNT = 5;
        // 两次ESC的最大间隔（秒）
        private const int ESC_INTERVAL_SECONDS = 2;

        /// <summary>
        /// 构造函数
        /// </summary>
        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        /// <summary>
        /// 启动钩子
        /// </summary>
        public void Start()
        {
            if (!_enabled)
            {
                _hookID = SetHook(_proc);
                _enabled = true;
                _escCount = 0;
            }
        }

        /// <summary>
        /// 停止钩子
        /// </summary>
        public void Stop()
        {
            if (_enabled)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                _enabled = false;
                _escCount = 0;
            }
        }

        /// <summary>
        /// 设置钩子
        /// </summary>
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// 钩子回调函数 - 处理键盘消息
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // 检查ESC键（用于紧急解锁）
                if (vkCode == VK_ESCAPE)
                {
                    HandleEscKey();
                }

                // 拦截Win键
                if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                {
                    return (IntPtr)1; // 拦截，不传递
                }

                // 拦截 Alt+Tab
                if ((Control.ModifierKeys & Keys.Alt) != 0 && vkCode == VK_TAB)
                {
                    return (IntPtr)1;
                }

                // 拦截 Ctrl+Esc（开始菜单）
                if ((Control.ModifierKeys & Keys.Control) != 0 && vkCode == VK_ESCAPE)
                {
                    return (IntPtr)1;
                }

                // 拦截 Ctrl+Alt+Delete （这个系统级的拦不住，但可以试试）
                if ((Control.ModifierKeys & Keys.Control) != 0 && 
                    (Control.ModifierKeys & Keys.Alt) != 0 && 
                    vkCode == VK_DELETE)
                {
                    // 这个通常拦不住，因为是系统安全快捷键
                    // 我们靠禁用任务管理器来兜底
                }

                // 拦截 Alt+F4
                if ((Control.ModifierKeys & Keys.Alt) != 0 && vkCode == (int)Keys.F4)
                {
                    return (IntPtr)1;
                }
            }

            // 其他按键正常传递
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 处理ESC键（检测连续按5次触发紧急解锁）
        /// </summary>
        private void HandleEscKey()
        {
            TimeSpan timeSinceLastEsc = DateTime.Now - _lastEscTime;

            if (timeSinceLastEsc.TotalSeconds <= ESC_INTERVAL_SECONDS)
            {
                // 在间隔内，计数+1
                _escCount++;
            }
            else
            {
                // 超过间隔了，重新计数
                _escCount = 1;
            }

            _lastEscTime = DateTime.Now;

            // 连续按够次数了，触发紧急解锁
            if (_escCount >= EMERGENCY_ESC_COUNT)
            {
                _escCount = 0;
                OnEmergencyUnlock();
            }
        }

        /// <summary>
        /// 触发紧急解锁事件
        /// </summary>
        protected virtual void OnEmergencyUnlock()
        {
            if (EmergencyUnlock != null)
            {
                EmergencyUnlock(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        #region Windows API 导入

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }
}
