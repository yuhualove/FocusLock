using System;
using System.Diagnostics;
using System.Timers;

namespace FocusLock
{
    /// <summary>
    /// 进程管理器 - 监控并关闭不在白名单的程序
    /// </summary>
    public class ProcessManager : IDisposable
    {
        // 监控定时器
        private Timer _monitorTimer;

        // 是否正在运行
        private bool _isRunning = false;

        // 被关闭的进程计数（用于统计）
        public int KilledCount { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProcessManager()
        {
            KilledCount = 0;
            _monitorTimer = new Timer(1000); // 每秒检查一次
            _monitorTimer.Elapsed += MonitorTimer_Elapsed;
            _monitorTimer.AutoReset = true;
        }

        /// <summary>
        /// 开始监控
        /// </summary>
        public void Start()
        {
            if (!_isRunning)
            {
                KilledCount = 0;
                _isRunning = true;
                _monitorTimer.Start();
            }
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void Stop()
        {
            if (_isRunning)
            {
                _isRunning = false;
                _monitorTimer.Stop();
            }
        }

        /// <summary>
        /// 定时检查进程
        /// </summary>
        private void MonitorTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_isRunning) return;

            try
            {
                KillUnauthorizedProcesses();
            }
            catch (Exception ex)
            {
                // 出错了也不能崩溃，继续运行
            }
        }

        /// <summary>
        /// 杀掉所有不在白名单里的进程
        /// </summary>
        public void KillUnauthorizedProcesses()
        {
            try
            {
                // 获取所有正在运行的进程
                Process[] allProcesses = Process.GetProcesses();

                foreach (Process proc in allProcesses)
                {
                    try
                    {
                        string procName = proc.ProcessName.ToLower();

                        // 如果不在白名单里，杀掉！
                        if (!ConfigHelper.IsInWhitelist(procName))
                        {
                            // 先确认进程还在运行
                            if (!proc.HasExited)
                            {
                                proc.Kill();
                                KilledCount++;
                            }
                        }
                    }
                    catch
                    {
                        // 有些系统进程不让杀，忽略就好
                    }
                }
            }
            catch (Exception ex)
            {
                // 获取进程列表失败也没关系
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Stop();
            if (_monitorTimer != null)
            {
                _monitorTimer.Dispose();
                _monitorTimer = null;
            }
        }
    }
}
