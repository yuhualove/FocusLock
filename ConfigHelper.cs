using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Drawing;

namespace FocusLock
{
    /// <summary>
    /// 白名单条目
    /// </summary>
    public class WhitelistItem
    {
        public string ProcessName { get; set; }  // 进程名（小写，用于匹配）
        public string DisplayName { get; set; }  // 显示名称（友好名字）
        public string ExePath { get; set; }      // exe路径（用于提取图标）

        public WhitelistItem()
        {
            ProcessName = "";
            DisplayName = "";
            ExePath = "";
        }

        public WhitelistItem(string processName)
        {
            ProcessName = processName.ToLower();
            DisplayName = processName;
            ExePath = "";
        }

        public WhitelistItem(AppInfo app)
        {
            ProcessName = app.ProcessName.ToLower();
            DisplayName = app.DisplayName;
            ExePath = app.ExePath;
        }

        /// <summary>
        /// 获取图标（如果有exe路径的话）
        /// </summary>
        public Icon GetIcon()
        {
            if (!string.IsNullOrEmpty(ExePath) && File.Exists(ExePath))
            {
                try
                {
                    return Icon.ExtractAssociatedIcon(ExePath);
                }
                catch { }
            }
            return null;
        }
    }

    /// <summary>
    /// 配置读写助手 - 管理白名单和默认设置
    /// </summary>
    public static class ConfigHelper
    {
        // 配置文件路径（和exe同目录）
        private static string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");

        // 白名单缓存
        private static List<WhitelistItem> _whitelist = new List<WhitelistItem>();

        // 默认专注时长
        private static int _defaultMinutes = 30;

        // 未完成的专注剩余秒数（用于误触紧急解锁后继续）
        private static int _remainingSeconds = 0;
        // 是否有未完成的专注
        private static bool _hasUnfinished = false;

        /// <summary>
        /// 静态构造函数：程序启动时自动加载配置
        /// </summary>
        static ConfigHelper()
        {
            LoadConfig();
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void LoadConfig()
        {
            try
            {
                // 如果配置文件不存在，创建默认的
                if (!File.Exists(ConfigPath))
                {
                    CreateDefaultConfig();
                    return;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(ConfigPath);

                // 读取默认时长
                XmlNode node = doc.SelectSingleNode("FocusLockConfig/DefaultMinutes");
                if (node != null)
                {
                    int.TryParse(node.InnerText, out _defaultMinutes);
                }

                // 读取未完成的专注剩余时间
                XmlNode unfinishedNode = doc.SelectSingleNode("FocusLockConfig/UnfinishedSeconds");
                if (unfinishedNode != null)
                {
                    int secs = 0;
                    if (int.TryParse(unfinishedNode.InnerText, out secs) && secs > 0)
                    {
                        _remainingSeconds = secs;
                        _hasUnfinished = true;
                    }
                }

                // 读取白名单
                _whitelist.Clear();
                XmlNodeList appNodes = doc.SelectNodes("FocusLockConfig/Whitelist/App");
                if (appNodes != null)
                {
                    foreach (XmlNode appNode in appNodes)
                    {
                        try
                        {
                            WhitelistItem item = new WhitelistItem();

                            // 进程名（兼容旧格式：直接是InnerText）
                            if (appNode.Attributes["ProcessName"] != null)
                            {
                                item.ProcessName = appNode.Attributes["ProcessName"].InnerText.Trim().ToLower();
                            }
                            else
                            {
                                item.ProcessName = appNode.InnerText.Trim().ToLower();
                            }

                            // 显示名
                            if (appNode.Attributes["DisplayName"] != null)
                            {
                                item.DisplayName = appNode.Attributes["DisplayName"].InnerText.Trim();
                            }
                            else
                            {
                                item.DisplayName = item.ProcessName;
                            }

                            // exe路径
                            if (appNode.Attributes["ExePath"] != null)
                            {
                                item.ExePath = appNode.Attributes["ExePath"].InnerText.Trim();
                            }

                            if (!string.IsNullOrEmpty(item.ProcessName))
                            {
                                _whitelist.Add(item);
                            }
                        }
                        catch { }
                    }
                }

                // 确保自己在白名单里
                if (!IsInWhitelist("focuslock"))
                {
                    _whitelist.Add(new WhitelistItem("focuslock") { DisplayName = "专注背稿锁机" });
                }
            }
            catch (Exception ex)
            {
                // 出错了就用默认配置
                _defaultMinutes = 30;
                CreateDefaultWhitelist();
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                
                // 根节点
                XmlElement root = doc.CreateElement("FocusLockConfig");
                doc.AppendChild(root);

                // 默认时长
                XmlElement defaultMin = doc.CreateElement("DefaultMinutes");
                defaultMin.InnerText = _defaultMinutes.ToString();
                root.AppendChild(defaultMin);

                // 未完成的专注剩余时间
                if (_hasUnfinished && _remainingSeconds > 0)
                {
                    XmlElement unfinished = doc.CreateElement("UnfinishedSeconds");
                    unfinished.InnerText = _remainingSeconds.ToString();
                    root.AppendChild(unfinished);
                }

                // 白名单
                XmlElement whitelist = doc.CreateElement("Whitelist");
                root.AppendChild(whitelist);

                foreach (WhitelistItem item in _whitelist)
                {
                    XmlElement appNode = doc.CreateElement("App");
                    appNode.SetAttribute("ProcessName", item.ProcessName);
                    appNode.SetAttribute("DisplayName", item.DisplayName);
                    if (!string.IsNullOrEmpty(item.ExePath))
                    {
                        appNode.SetAttribute("ExePath", item.ExePath);
                    }
                    whitelist.AppendChild(appNode);
                }

                doc.Save(ConfigPath);
            }
            catch
            {
                // 保存失败就算了
            }
        }

        /// <summary>
        /// 获取白名单列表
        /// </summary>
        public static List<WhitelistItem> GetWhitelist()
        {
            return new List<WhitelistItem>(_whitelist);
        }

        /// <summary>
        /// 检查某个进程名是否在白名单里
        /// </summary>
        public static bool IsInWhitelist(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return false;
            string name = processName.Trim().ToLower();
            foreach (WhitelistItem item in _whitelist)
            {
                if (item.ProcessName == name) return true;
            }
            return false;
        }

        /// <summary>
        /// 添加软件到白名单（从AppInfo添加，信息最全）
        /// </summary>
        public static void AddToWhitelist(AppInfo app)
        {
            if (app == null || string.IsNullOrEmpty(app.ProcessName)) return;
            string name = app.ProcessName.ToLower();
            if (IsInWhitelist(name)) return;

            WhitelistItem item = new WhitelistItem(app);
            _whitelist.Add(item);
            SaveConfig();
        }

        /// <summary>
        /// 添加进程名到白名单（简单方式）
        /// </summary>
        public static void AddToWhitelist(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;
            string name = processName.Trim().ToLower();
            if (IsInWhitelist(name)) return;

            _whitelist.Add(new WhitelistItem(name));
            SaveConfig();
        }

        /// <summary>
        /// 从白名单移除
        /// </summary>
        public static void RemoveFromWhitelist(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;
            string name = processName.Trim().ToLower();
            // 不能移除自己
            if (name == "focuslock") return;

            for (int i = _whitelist.Count - 1; i >= 0; i--)
            {
                if (_whitelist[i].ProcessName == name)
                {
                    _whitelist.RemoveAt(i);
                }
            }
            SaveConfig();
        }

        /// <summary>
        /// 获取/设置默认专注时长
        /// </summary>
        public static int DefaultMinutes
        {
            get { return _defaultMinutes; }
            set { _defaultMinutes = value; SaveConfig(); }
        }

        /// <summary>
        /// 是否有未完成的专注（误触紧急解锁后可以继续）
        /// </summary>
        public static bool HasUnfinishedFocus
        {
            get { return _hasUnfinished && _remainingSeconds > 0; }
        }

        /// <summary>
        /// 未完成专注的剩余秒数
        /// </summary>
        public static int RemainingSeconds
        {
            get { return _remainingSeconds; }
        }

        /// <summary>
        /// 保存未完成的专注（紧急解锁时调用）
        /// </summary>
        public static void SaveUnfinishedFocus(int remainingSeconds)
        {
            _remainingSeconds = remainingSeconds;
            _hasUnfinished = true;
            SaveConfig();
        }

        /// <summary>
        /// 清除未完成的专注（正常完成或用户选择重新开始时调用）
        /// </summary>
        public static void ClearUnfinishedFocus()
        {
            _remainingSeconds = 0;
            _hasUnfinished = false;
            SaveConfig();
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private static void CreateDefaultConfig()
        {
            _defaultMinutes = 30;
            CreateDefaultWhitelist();
            SaveConfig();
        }

        /// <summary>
        /// 创建默认白名单
        /// </summary>
        private static void CreateDefaultWhitelist()
        {
            _whitelist = new List<WhitelistItem>();

            // WPS
            AddItemNoSave("wps", "WPS文字", "");
            AddItemNoSave("et", "WPS表格", "");
            AddItemNoSave("wpp", "WPS演示", "");
            AddItemNoSave("ksolaunch", "WPS启动器", "");
            AddItemNoSave("wpscenter", "WPS中心", "");

            // 汽水音乐
            AddItemNoSave("sodamusic", "汽水音乐", "");

            // 自己
            AddItemNoSave("focuslock", "专注背稿锁机", "");

            // 系统关键进程
            AddItemNoSave("explorer", "资源管理器", "");
            AddItemNoSave("svchost", "系统服务", "");
            AddItemNoSave("winlogon", "登录进程", "");
            AddItemNoSave("csrss", "系统进程", "");
            AddItemNoSave("smss", "系统进程", "");
            AddItemNoSave("services", "服务控制管理器", "");
            AddItemNoSave("lsass", "安全认证服务", "");
            AddItemNoSave("dwm", "桌面窗口管理器", "");
            AddItemNoSave("conhost", "控制台窗口主机", "");
            AddItemNoSave("searchhost", "搜索", "");
            AddItemNoSave("startmenuexperiencehost", "开始菜单", "");
            AddItemNoSave("textinputhost", "文本输入", "");
            AddItemNoSave("runtimebroker", "运行时代理", "");
            AddItemNoSave("shellexperiencehost", "外壳体验", "");
            AddItemNoSave("windowsterminal", "Windows终端", "");
            AddItemNoSave("taskhostw", "任务主机", "");
            AddItemNoSave("ctfmon", "输入法", "");
            AddItemNoSave("audiodg", "音频", "");
            AddItemNoSave("fontdrvhost", "字体驱动", "");
            AddItemNoSave("nvcontainer", "NVIDIA容器", "");
            AddItemNoSave("nvvsvc", "NVIDIA服务", "");
            AddItemNoSave("nvxdsync", "NVIDIA同步", "");
            AddItemNoSave("sihost", "Shell基础结构", "");
            AddItemNoSave("securityhealthservice", "安全中心服务", "");
            AddItemNoSave("securityhealthsystray", "安全中心图标", "");
            AddItemNoSave("msmpeng", "Windows Defender", "");
            AddItemNoSave("nissrv", "网络检查服务", "");
        }

        private static void AddItemNoSave(string procName, string displayName, string exePath)
        {
            _whitelist.Add(new WhitelistItem(procName)
            {
                DisplayName = displayName,
                ExePath = exePath
            });
        }
    }
}
