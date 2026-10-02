using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace FocusLock
{
    /// <summary>
    /// 软件信息
    /// </summary>
    public class AppInfo
    {
        public string DisplayName { get; set; }   // 显示名称（比如"汽水音乐"）
        public string ProcessName { get; set; }   // 进程名（比如"sodamusic"）
        public string ExePath { get; set; }       // exe完整路径
        public Icon AppIcon { get; set; }         // 图标

        public AppInfo()
        {
            DisplayName = "";
            ProcessName = "";
            ExePath = "";
            AppIcon = null;
        }
    }

    /// <summary>
    /// 软件扫描器 - 智能扫描电脑上的软件
    /// </summary>
    public static class AppScanner
    {
        #region 1. 扫描已安装的软件（从开始菜单 + 桌面快捷方式）

        /// <summary>
        /// 扫描所有已安装的软件（从快捷方式扫描）
        /// </summary>
        public static List<AppInfo> ScanInstalledApps()
        {
            Dictionary<string, AppInfo> apps = new Dictionary<string, AppInfo>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // 要扫描的目录：开始菜单（当前用户+所有用户）+ 桌面
                List<string> scanDirs = new List<string>();

                // 当前用户开始菜单
                string userStartMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs");
                if (Directory.Exists(userStartMenu)) scanDirs.Add(userStartMenu);

                // 所有用户开始菜单
                string commonStartMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                    "Programs");
                if (Directory.Exists(commonStartMenu)) scanDirs.Add(commonStartMenu);

                // 当前用户桌面
                string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (Directory.Exists(userDesktop)) scanDirs.Add(userDesktop);

                // 公共桌面
                string commonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                if (Directory.Exists(commonDesktop)) scanDirs.Add(commonDesktop);

                // 扫描每个目录里的 .lnk 快捷方式
                foreach (string dir in scanDirs)
                {
                    ScanShortcutsInDir(dir, apps);
                }

                // 再从注册表补充一些（卸载列表里的）
                ScanFromRegistry(apps);
            }
            catch
            {
                // 扫描失败就算了
            }

            return new List<AppInfo>(apps.Values);
        }

        /// <summary>
        /// 扫描目录里的快捷方式
        /// </summary>
        private static void ScanShortcutsInDir(string dir, Dictionary<string, AppInfo> apps)
        {
            try
            {
                // 先扫描当前目录的 .lnk
                string[] lnkFiles = Directory.GetFiles(dir, "*.lnk", SearchOption.TopDirectoryOnly);
                foreach (string lnk in lnkFiles)
                {
                    try
                    {
                        AppInfo app = ResolveShortcut(lnk);
                        if (app != null && !string.IsNullOrEmpty(app.ProcessName))
                        {
                            if (!apps.ContainsKey(app.ProcessName))
                            {
                                apps[app.ProcessName] = app;
                            }
                            else if (apps[app.ProcessName].AppIcon == null && app.AppIcon != null)
                            {
                                // 如果已有的没图标，用这个的
                                apps[app.ProcessName].AppIcon = app.AppIcon;
                            }
                        }
                    }
                    catch { }
                }

                // 递归子目录
                string[] subDirs = Directory.GetDirectories(dir);
                foreach (string subDir in subDirs)
                {
                    // 跳过一些没用的
                    string dirName = Path.GetFileName(subDir).ToLower();
                    if (dirName == "启动" || dirName == "startup" || dirName == "附件" ||
                        dirName == "accessories" || dirName == "系统工具")
                        continue;

                    ScanShortcutsInDir(subDir, apps);
                }
            }
            catch { }
        }

        /// <summary>
        /// 解析快捷方式（.lnk文件）
        /// </summary>
        private static AppInfo ResolveShortcut(string lnkPath)
        {
            try
            {
                // 使用 WScript.Shell COM 对象解析快捷方式
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod, null, shell,
                    new object[] { lnkPath });

                // 获取目标路径
                string targetPath = (string)shortcut.GetType().InvokeMember("TargetPath",
                    System.Reflection.BindingFlags.GetProperty, null, shortcut, null);

                // 获取工作目录
                string workingDir = (string)shortcut.GetType().InvokeMember("WorkingDirectory",
                    System.Reflection.BindingFlags.GetProperty, null, shortcut, null);

                // 释放COM对象
                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);

                if (string.IsNullOrEmpty(targetPath)) return null;
                if (!File.Exists(targetPath)) return null;
                if (!targetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return null;

                // 构建AppInfo
                AppInfo app = new AppInfo();
                app.ExePath = targetPath;
                app.ProcessName = Path.GetFileNameWithoutExtension(targetPath).ToLower();
                app.DisplayName = Path.GetFileNameWithoutExtension(lnkPath); // 快捷方式名字作为显示名

                // 提取图标
                try
                {
                    app.AppIcon = Icon.ExtractAssociatedIcon(targetPath);
                }
                catch { }

                return app;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从注册表补充已安装软件（卸载列表）
        /// </summary>
        private static void ScanFromRegistry(Dictionary<string, AppInfo> apps)
        {
            try
            {
                string[] regPaths = {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (string regPath in regPaths)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath))
                    {
                        if (key == null) continue;

                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey appKey = key.OpenSubKey(subKeyName))
                                {
                                    if (appKey == null) continue;

                                    string displayName = appKey.GetValue("DisplayName") as string;
                                    string displayIcon = appKey.GetValue("DisplayIcon") as string;

                                    if (string.IsNullOrEmpty(displayName)) continue;

                                    // 找exe路径
                                    string exePath = "";
                                    if (!string.IsNullOrEmpty(displayIcon))
                                    {
                                        // DisplayIcon可能是 "path.exe,0" 格式
                                        if (displayIcon.Contains(","))
                                        {
                                            exePath = displayIcon.Split(',')[0].Trim();
                                        }
                                        else
                                        {
                                            exePath = displayIcon.Trim();
                                        }
                                        // 去掉引号
                                        exePath = exePath.Trim('"', '\'');
                                    }

                                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath) ||
                                        !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    string procName = Path.GetFileNameWithoutExtension(exePath).ToLower();
                                    if (apps.ContainsKey(procName)) continue;

                                    AppInfo app = new AppInfo();
                                    app.DisplayName = displayName;
                                    app.ProcessName = procName;
                                    app.ExePath = exePath;
                                    try
                                    {
                                        app.AppIcon = Icon.ExtractAssociatedIcon(exePath);
                                    }
                                    catch { }

                                    apps[procName] = app;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        #endregion

        #region 2. 扫描正在运行的程序（有窗口的）

        // Windows API 委托
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd); // 是否最小化

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        private const int WM_GETTEXT = 0x000D;
        private const int WM_GETTEXTLENGTH = 0x000E;

        /// <summary>
        /// 扫描正在运行的、有主窗口的程序
        /// </summary>
        public static List<AppInfo> ScanRunningApps()
        {
            Dictionary<uint, AppInfo> apps = new Dictionary<uint, AppInfo>();

            try
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    try
                    {
                        // 只看可见的窗口
                        if (!IsWindowVisible(hWnd)) return true;

                        // 获取窗口标题
                        StringBuilder title = new StringBuilder(256);
                        GetWindowText(hWnd, title, 256);
                        if (string.IsNullOrWhiteSpace(title.ToString())) return true;

                        // 跳过一些系统窗口
                        string titleStr = title.ToString().Trim();
                        if (titleStr == "Program Manager" || titleStr == "程序管理器") return true;
                        if (titleStr == "任务切换" || titleStr == "Task Switching") return true;
                        if (titleStr == "Windows 输入体验") return true;

                        // 获取进程ID
                        uint procId;
                        GetWindowThreadProcessId(hWnd, out procId);
                        if (procId == 0) return true;

                        // 已经加过这个进程了，跳过
                        if (apps.ContainsKey(procId)) return true;

                        // 获取进程信息
                        Process proc = Process.GetProcessById((int)procId);
                        if (proc == null) return true;

                        string procName = proc.ProcessName.ToLower();
                        string exePath = "";
                        try
                        {
                            exePath = proc.MainModule.FileName;
                        }
                        catch { }

                        AppInfo app = new AppInfo();
                        app.DisplayName = titleStr;
                        app.ProcessName = procName;
                        app.ExePath = exePath;

                        // 提取图标
                        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                        {
                            try
                            {
                                app.AppIcon = Icon.ExtractAssociatedIcon(exePath);
                            }
                            catch { }
                        }

                        apps[procId] = app;
                    }
                    catch { }

                    return true;
                }, IntPtr.Zero);
            }
            catch { }

            return new List<AppInfo>(apps.Values);
        }

        #endregion

        #region 3. 从exe文件提取信息

        /// <summary>
        /// 从exe文件路径提取软件信息
        /// </summary>
        public static AppInfo GetAppFromExe(string exePath)
        {
            if (string.IsNullOrEmpty(exePath)) return null;
            if (!File.Exists(exePath)) return null;
            if (!exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return null;

            AppInfo app = new AppInfo();
            app.ExePath = exePath;
            app.ProcessName = Path.GetFileNameWithoutExtension(exePath).ToLower();

            // 尝试从文件版本信息获取显示名
            try
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(exePath);
                if (!string.IsNullOrEmpty(fvi.ProductName))
                {
                    app.DisplayName = fvi.ProductName;
                }
                else if (!string.IsNullOrEmpty(fvi.FileDescription))
                {
                    app.DisplayName = fvi.FileDescription;
                }
                else
                {
                    app.DisplayName = Path.GetFileNameWithoutExtension(exePath);
                }
            }
            catch
            {
                app.DisplayName = Path.GetFileNameWithoutExtension(exePath);
            }

            // 提取图标
            try
            {
                app.AppIcon = Icon.ExtractAssociatedIcon(exePath);
            }
            catch { }

            return app;
        }

        #endregion
    }
}
