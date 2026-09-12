using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace GrantIniAccess
{
    /// <summary>
    /// 环境探测器：把"ACL 改对了却依然写不进"的非权限类拦截全部查出来。
    ///
    /// ★ 为什么需要这个：
    /// 权限开通失败时，现场最常见的误判是"ACL 没改好"。实际上 Windows 上至少有 6 类
    /// 拦截与 ACL 无关（杀软/受控文件夹访问、文件独占、EFS 加密、UAC 虚拟化、
    /// 网络路径、磁盘只读）。不查清这些，改一百遍 ACL 也没用。
    ///
    /// 本类只做"探测"，不修改文件/ACL（例外：TryAddToDefenderAllowed 写注册表；
    /// BuildReport 会调用 PermissionHelper.EnsurePrivileges 启用进程特权）。
    /// </summary>
    public static class EnvironmentProbe
    {
        #region 文件占用：Restart Manager（能报出是哪个进程占着）

        private const int RmRebootReasonNone = 0;
        private const int CCH_RM_MAX_APP_NAME = 255;
        private const int CCH_RM_MAX_SVC_NAME = 63;
        private const int ERROR_MORE_DATA = 234;

        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        private enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;
            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmRegisterResources(
            uint pSessionHandle,
            uint nFiles,
            string[] rgsFilenames,
            uint nApplications,
            [In] RM_UNIQUE_PROCESS[] rgApplications,
            uint nServices,
            string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmGetList(
            uint dwSessionHandle,
            out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);

        /// <summary>
        /// ★ 核心：列出正在占用该文件的进程（名称 + PID）。
        /// 这是"改了 ACL 还是写不进"最常见的真凶 —— 主软件自己占着 ini。
        /// 用 Windows 官方 Restart Manager，比遍历句柄表稳定得多。
        /// </summary>
        public static List<string> GetLockingProcesses(string path)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;

            uint session;
            int err = RmStartSession(out session, 0, Guid.NewGuid().ToString("N"));
            if (err != 0) return result;

            try
            {
                string[] resources = { path };
                err = RmRegisterResources(session, 1, resources, 0, null, 0, null);
                if (err != 0) return result;

                uint needed = 0, count = 0, reboot = RmRebootReasonNone;
                // 第一次取值数量
                err = RmGetList(session, out needed, ref count, null, ref reboot);
                if (err != ERROR_MORE_DATA && err != 0) return result;
                if (needed == 0) return result;

                count = needed;
                var procInfo = new RM_PROCESS_INFO[needed];
                err = RmGetList(session, out needed, ref count, procInfo, ref reboot);
                if (err != 0) return result;

                for (int i = 0; i < count && i < procInfo.Length; i++)
                {
                    string name = string.IsNullOrEmpty(procInfo[i].strAppName)
                        ? "(未知)" : procInfo[i].strAppName;
                    result.Add(name + "  [PID " + procInfo[i].Process.dwProcessId + "]");
                }
            }
            finally
            {
                try { RmEndSession(session); } catch { }
            }

            return result;
        }

        /// <summary>
        /// ★ P2：列出"正在占用该目录内文件"的进程（名称 + PID）。
        /// Restart Manager 不支持直接查询目录，这里扫描目录内的文件（TopDirectoryOnly，有数量上限）
        /// 逐个查询后去重。仅用于诊断展示 —— 目录被占用并不妨碍修改 ACL。
        /// </summary>
        public static List<string> GetLockingProcessesForDirectory(string dirPath, int maxFiles = 300)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return result;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int scanned = 0;
            try
            {
                foreach (string file in Directory.EnumerateFiles(dirPath, "*", SearchOption.TopDirectoryOnly))
                {
                    if (scanned >= maxFiles) break;
                    scanned++;
                    foreach (string item in GetLockingProcesses(file))
                    {
                        if (seen.Add(item)) result.Add(item);
                    }
                }
            }
            catch { }
            return result;
        }

        #endregion

        #region Defender 受控文件夹访问（Controlled Folder Access）

        private const string CfaPolicyKey =
            @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access";
        private const string CfaRuntimeKey =
            @"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access";

        /// <summary>CFA 状态。</summary>
        public enum CfaState { Disabled = 0, Enabled = 1, Audit = 2, Unknown = 99 }

        public static CfaState GetCfaState()
        {
            foreach (string keyPath in new[] { CfaPolicyKey, CfaRuntimeKey })
            {
                try
                {
                    using (var k = Registry.LocalMachine.OpenSubKey(keyPath))
                    {
                        if (k == null) continue;
                        object v = k.GetValue("EnableControlledFolderAccess");
                        if (v == null) continue;
                        int n;
                        if (int.TryParse(v.ToString(), out n))
                            return (CfaState)n;
                    }
                }
                catch { }
            }
            return CfaState.Unknown;
        }

        /// <summary>受控文件夹列表（含默认用户目录）。</summary>
        public static List<string> GetProtectedFolders()
        {
            var list = new List<string>();
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(CfaRuntimeKey))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("ControlledFolders");
                        var multi = v as string[];
                        if (multi != null) list.AddRange(multi);
                    }
                }
            }
            catch { }

            // 默认受保护的用户目录（CFA 未显式配置时也会保护这些）
            // ★ P0 修复：以前用 Enum.Parse 字符串，"Documents/Pictures/Videos/Music" 在
            // Environment.SpecialFolder 中并不存在（正确名是 MyDocuments/MyPictures/MyVideos/MyMusic），
            // 解析抛异常被吞掉 → 这 4 个目录实际从未被检测。这里改为直接使用枚举成员。
            foreach (Environment.SpecialFolder sf in new[]
            {
                Environment.SpecialFolder.Desktop,
                Environment.SpecialFolder.MyDocuments,
                Environment.SpecialFolder.MyPictures,
                Environment.SpecialFolder.MyVideos,
                Environment.SpecialFolder.MyMusic,
                Environment.SpecialFolder.Favorites
            })
            {
                try
                {
                    string p = Environment.GetFolderPath(sf);
                    if (!string.IsNullOrEmpty(p) && !list.Contains(p)) list.Add(p);
                }
                catch { }
            }
            return list;
        }

        /// <summary>目标路径是否落在受保护目录下。</summary>
        public static bool IsPathProtectedByCfa(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                string full = Path.GetFullPath(path).TrimEnd('\\').ToLowerInvariant();
                foreach (string folder in GetProtectedFolders())
                {
                    if (string.IsNullOrEmpty(folder)) continue;
                    string f = folder.TrimEnd('\\').ToLowerInvariant();
                    if (full.Equals(f) || full.StartsWith(f + "\\")) return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// ★ 尝试把指定 exe 加入 CFA 允许列表（需要管理员）。
        /// 成功返回 true；被 Defender 篡改保护拒绝时返回 false（此时只能手动加）。
        /// </summary>
        public static bool TryAddToDefenderAllowed(string exePath, out string message)
        {
            message = "";
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                message = "exe 不存在: " + exePath;
                return false;
            }

            try
            {
                using (var k = Registry.LocalMachine.CreateSubKey(CfaPolicyKey))
                {
                    if (k == null)
                    {
                        message = "无法打开 CFA 策略注册表项（可能无权限）";
                        return false;
                    }

                    var current = k.GetValue("AllowedApplications") as string[] ?? new string[0];
                    var set = new List<string>(current);
                    if (!set.Contains(exePath))
                    {
                        set.Add(exePath);
                        k.SetValue("AllowedApplications", set.ToArray(), RegistryValueKind.MultiString);
                    }
                    message = "已加入允许列表:\r\n" + exePath + "\r\n（若 Defender 开了篡改保护，需到 Windows 安全中心手动确认）";
                    return true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                message = "写入 CFA 允许列表被拒（需管理员，或注册表受保护）: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                message = "写入 CFA 允许列表失败: " + ex.Message;
                return false;
            }
        }

        #endregion

        #region 其它拦截项探测

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern bool PathIsNetworkPath(string pszPath);

        /// <summary>是否网络路径（含映射盘符）——权限由远端服务器决定。</summary>
        public static bool IsNetworkPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try { return PathIsNetworkPath(Path.GetFullPath(path)); }
            catch { return path.StartsWith(@"\\"); }
        }

        /// <summary>是否 EFS 加密（不是权限问题，是证书问题）。</summary>
        public static bool IsEfsEncrypted(string path)
        {
            try
            {
                if (File.Exists(path))
                    return (File.GetAttributes(path) & FileAttributes.Encrypted) != 0;
                if (Directory.Exists(path))
                    return (new DirectoryInfo(path).Attributes & FileAttributes.Encrypted) != 0;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// ★ UAC 虚拟化影子副本：非提权程序写 Program Files 会被静默重定向到
        /// %LOCALAPPDATA%\VirtualStore。表现为"写入成功但主程序读不到"。
        /// </summary>
        public static string GetVirtualStoreShadow(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return null;
                string full = Path.GetFullPath(path);
                string root = Path.GetPathRoot(full);
                if (string.IsNullOrEmpty(root)) return null;

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string relative = full.Substring(root.Length);           // 去掉盘符
                string shadow = Path.Combine(localAppData, "VirtualStore", relative.TrimStart('\\'));

                return (File.Exists(shadow) || Directory.Exists(shadow)) ? shadow : null;
            }
            catch { return null; }
        }

        /// <summary>磁盘是否可写（只读介质 / 磁盘满 / 未就绪）。返回 null 表示正常。</summary>
        public static string GetDiskProblem(string path)
        {
            try
            {
                string root = Path.GetPathRoot(Path.GetFullPath(path));
                if (string.IsNullOrEmpty(root)) return null;
                var di = new DriveInfo(root);
                if (!di.IsReady) return "磁盘未就绪: " + root;
                if (di.AvailableFreeSpace < 1024 * 1024)
                    return "磁盘剩余空间不足 1MB，无法写入: " + root;
            }
            catch (Exception ex) { return "磁盘检查失败: " + ex.Message; }
            return null;
        }

        /// <summary>目录路径长度是否超过 248（接近 MAX_PATH 260 时会失效）。</summary>
        public static bool IsPathTooLong(string path)
        {
            try { return Path.GetFullPath(path).Length > 248; } catch { return false; }
        }

        /// <summary>检测常见杀软 / EDR 进程（它们可能拦截写入）。</summary>
        public static List<string> DetectAntivirus()
        {
            var found = new List<string>();
            string[] known =
            {
                "360Tray","360Safe","ZhuDongFangYu","HipsTray",       // 360
                "HuorongSysdiag","hipsdaemon","HRSword",             // 火绒
                "avp","klnagent",                                     // 卡巴斯基
                "ccSvcHst","smc","SmcGui",                            // 赛门铁克
                "Mcshield","mcconsol",                                // McAfee
                "MsMpEng","NisSrv",                                   // Defender
                "TMBMSRV","TmListen",                                 // 趋势
                "RavMonD","RavTask",                                  // 瑞星
                "QQPCRTP","QQPCTray",                                 // 电脑管家
                "V3Svc","AhnSD",                                      // AhnLab
                "Sophos","ekrn","egui",                               // Sophos / ESET
                "CSFalcon","CbDefense","SentinelAgent"                // EDR
            };
            try
            {
                var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var processes = Process.GetProcesses();
                try
                {
                    foreach (var p in processes)
                    {
                        try { running.Add(p.ProcessName); } catch { }
                    }
                }
                finally
                {
                    foreach (var p in processes) { try { p.Dispose(); } catch { } }
                }
                foreach (string name in known)
                {
                    if (running.Contains(name)) found.Add(name);
                }
            }
            catch { }
            return found;
        }

        #endregion

        #region 综合报告

        /// <summary>生成一份完整的环境诊断报告（只读，不改任何东西）。</summary>
        public static string BuildReport(string targetPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== 环境诊断报告 =====");
            sb.AppendLine("目标: " + (string.IsNullOrEmpty(targetPath) ? "(未选择)" : targetPath));
            sb.AppendLine("时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            // 1. 管理员
            bool admin = PermissionHelper.IsAdministrator();
            sb.AppendLine("[1] 管理员权限: " + (admin ? "是" : "★否 —— 必须提权"));

            // 2. 特权
            string priv = PermissionHelper.EnsurePrivileges();
            bool hasTake = priv.Contains("SeTakeOwnershipPrivilege: 已启用");
            sb.AppendLine("[2] 夺取所有权特权(SeTakeOwnership): " + (hasTake ? "已启用" : "★未启用 —— Owner 为 TrustedInstaller 时会失败"));
            sb.AppendLine(priv);

            // 3. 存在性
            bool isDir = Directory.Exists(targetPath);
            bool isFile = File.Exists(targetPath);
            sb.AppendLine("[3] 目标存在: " + (isFile ? "是(文件)" : isDir ? "是(目录)" : "★否"));
            if (!isFile && !isDir)
            {
                string parent = null;
                try { parent = Path.GetDirectoryName(targetPath); } catch { }
                sb.AppendLine("    父目录: " + parent + (Directory.Exists(parent) ? " (存在)" : " ★不存在"));
            }

            // 4. 占用
            if (isFile)
            {
                var lockers = GetLockingProcesses(targetPath);
                if (lockers.Count > 0)
                {
                    sb.AppendLine("[4] 文件占用: ★被以下进程占用（改 ACL 无效，必须先关闭）");
                    foreach (var l in lockers) sb.AppendLine("      - " + l);
                }
                else sb.AppendLine("[4] 文件占用: 无");
            }
            else if (isDir)
            {
                var dirLockers = GetLockingProcessesForDirectory(targetPath);
                if (dirLockers.Count > 0)
                {
                    sb.AppendLine("[4] 目录内文件占用: 以下进程正在使用目录内的文件（通常不影响改 ACL，但写 ini 可能失败）");
                    foreach (var l in dirLockers) sb.AppendLine("      - " + l);
                }
                else sb.AppendLine("[4] 目录内文件占用: 无");
            }

            // 5. Defender CFA
            var state = GetCfaState();
            bool protectedByCfa = IsPathProtectedByCfa(targetPath);
            sb.AppendLine("[5] Defender 受控文件夹访问: " + state +
                (protectedByCfa ? "  ★目标在受保护目录内 —— 需把主程序加入允许列表" : "  目标不在受保护目录"));
            if (protectedByCfa)
            {
                foreach (var f in GetProtectedFolders())
                    sb.AppendLine("      受保护目录: " + f);
            }

            // 6. 杀软
            var av = DetectAntivirus();
            sb.AppendLine("[6] 杀软/EDR 进程: " + (av.Count > 0 ? string.Join(", ", av) + "  （可能拦截写入，建议加白名单）" : "未检出"));

            // 7. EFS
            sb.AppendLine("[7] EFS 加密: " + (IsEfsEncrypted(targetPath) ? "★是 —— 属证书问题，非权限问题" : "否"));

            // 8. 网络路径
            sb.AppendLine("[8] 网络路径: " + (IsNetworkPath(targetPath) ? "★是 —— 权限由远端服务器决定" : "否"));

            // 9. UAC 虚拟化
            string shadow = GetVirtualStoreShadow(targetPath);
            sb.AppendLine("[9] UAC 虚拟化影子: " + (shadow != null
                ? "★存在 —— " + shadow + "\r\n      说明曾有非提权程序写入被重定向，主程序可能读到影子而非真实文件"
                : "无"));

            // 10. 磁盘
            string disk = GetDiskProblem(targetPath);
            sb.AppendLine("[10] 磁盘: " + (disk ?? "正常"));

            // 11. 路径长度
            sb.AppendLine("[11] 路径长度: " + (IsPathTooLong(targetPath) ? "★超过 248，可能触发 MAX_PATH 限制" : "正常"));

            // 12. 只读属性
            if (isFile)
            {
                try
                {
                    bool ro = (File.GetAttributes(targetPath) & FileAttributes.ReadOnly) != 0;
                    sb.AppendLine("[12] 只读属性: " + (ro ? "★是（工具会自动清除）" : "否"));
                }
                catch { }
            }

            // 13. 所有者
            try
            {
                if (isFile || isDir)
                {
                    System.Security.Principal.SecurityIdentifier owner = null;
                    if (isFile)
                        owner = new FileInfo(targetPath).GetAccessControl().GetOwner(typeof(System.Security.Principal.SecurityIdentifier))
                            as System.Security.Principal.SecurityIdentifier;
                    else
                        owner = new DirectoryInfo(targetPath).GetAccessControl().GetOwner(typeof(System.Security.Principal.SecurityIdentifier))
                            as System.Security.Principal.SecurityIdentifier;

                    string ownerName = "?";
                    if (owner != null)
                    {
                        try { ownerName = owner.Translate(typeof(System.Security.Principal.NTAccount)).Value; }
                        catch { ownerName = owner.Value; }
                    }
                    sb.AppendLine("[13] 当前所有者: " + ownerName);
                }
            }
            catch { }

            sb.AppendLine();
            sb.AppendLine("===== 报告结束 =====");
            return sb.ToString();
        }

        #endregion
    }
}