using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace GrantIniAccess
{
    /// <summary>
    /// 权限开通工具核心逻辑：处理 ini 路径读写权限、对目标文件/目录彻底开放 ACL。
    ///
    /// 设计原则（2026-09-07 完整优化）：
    /// 1. 覆盖三类常见场景：硬编码内置 test.ini（C:\Program Files\test.ini）、任意目录、
    ///    任意具体文件（包括应用程序目录下的 code.ini，正是加密流程写加密码时报错的文件）。
    /// 2. 先诊断（Diagnose）后开通：区分"目录不存在 / 权限被拒 / 只读 / 文件被占用 /
    ///    Owner 不是管理员"，给现场人员明确指引，而不是只给一行 Exception 文本。
    /// 3. 开通后实测（TestWriteAccess / TestDirectoryWriteAccess）：真的创建-写-删一个临时
    ///    文件，验证修复有效，避免"看着像成功但实际还是写不进"。
    /// 4. takeown 兜底：若管理员因非 Owner 无法改 ACL，提供 /takeown 提示；不能默默失败。
    /// </summary>
    public static class PermissionHelper
    {
        #region 进度上报（供 UI 进度条使用）

        /// <summary>
        /// 处理进度。参数：current 当前百分比、total 固定为 100、message 当前说明。
        /// （无论是文件模式的分阶段上报，还是目录模式"已处理项数 / 总项数"换算后的比例，
        ///   统一以 0~100 的百分比形式上报。）
        /// 事件在后台工作线程触发，订阅方需自行切回 UI 线程。
        /// </summary>
        public static event Action<int, int, string> ProgressChanged;

        private static void ReportProgress(int current, int total, string message)
        {
            var handler = ProgressChanged;
            if (handler != null)
            {
                try { handler(current, total, message); }
                catch { }
            }
        }

        #endregion

        #region 管理员检测 / 自提升

        public static bool IsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch { return false; }
        }

        public static void RestartAsAdministrator()
        {
            var psi = new ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath)
            };
            Process.Start(psi);
        }

        #endregion

        #region 对外入口（保持兼容 + 新增）

        /// <summary>★ 2026-09-07 新增：开通任意指定 ini 文件的权限（覆盖 code.ini 等场景）。
        /// 自动开通文件 + 父目录 + 若父目录缺失则创建。</summary>
        public static string GrantSpecificFileAccess(string filePath)
        {
            return GrantConfigFileAccess(filePath, showPathInMessage: true);
        }

        /// <summary>目录开通：递归处理子目录与文件（含权限继承）。</summary>
        public static string GrantDirectoryAccess(string dirPath)
        {
            if (string.IsNullOrWhiteSpace(dirPath))
                return "目录路径不能为空。";

            dirPath = Path.GetFullPath(dirPath.Trim());
            if (!Directory.Exists(dirPath))
                return "目录不存在:\r\n" + dirPath;

            string preCheck = PreCheckTarget(dirPath);
            if (preCheck != null) return preCheck;
            BackupAcl(dirPath);

            try
            {
                Exception lastErr;
                // 1) 目录自身 ACL 快速设置，失败才重试（整棵树不在重试范围内）
                bool done = Retry(() =>
                {
                    ReportProgress(5, 100, "正在设置目录自身权限...");
                    ApplyFullAccessToDirectoryOnly(dirPath);
                }, 3, out lastErr);
                if (!done)
                {
                    bool restored = RestoreAcl(dirPath);
                    RestoreOwners();
                    string hint = EnvironmentProbe.IsPathProtectedByCfa(dirPath)
                        ? "\r\n\r提示：目标受 Windows Defender「受控文件夹访问」保护，请把主程序加入允许列表。"
                        : "";
                    return "开通失败（已重试 3 次，原始 ACL 已还原）:\r\n"
                        + (lastErr != null ? lastErr.Message : "") + hint
                        + (restored ? "" : "\r\n★ 注意：原始 ACL 还原失败，请手动检查该目录权限。");
                }
                // 2) 树遍历子项（逐层容错 + 进度上报）
                int failed = ApplyFullAccessToDirectoryTree(dirPath);
                // 3) 把本次夺取过的所有者还原（ACL 授权保留）
                RestoreOwners();
                return "开通成功（指定目录）\r\n已为目录开通完整读写权限（含全部子项）:\r\n" + dirPath
                    + (failed > 0
                        ? "\r\n\r\n★ 注意：有 " + failed + " 个子项因权限不足被跳过，请对其单独处理。"
                        : "");
            }
            catch (UnauthorizedAccessException ex)
            {
                return "开通失败（权限被拒）: " + ex.Message
                    + "\r\n\r多半是目录 Owner 不是当前管理员，"
                    + "请先在管理员 cmd 中执行：\r\ntakeown /F \"" + dirPath + "\" /R /D Y";
            }
            catch (Exception ex)
            {
                return "开通失败:\r\n" + ex.Message;
            }
        }

        #endregion

        #region 核心：开通任意文件权限

        private static string GrantConfigFileAccess(string filePath, bool showPathInMessage)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "文件路径不能为空。";

            try
            {
                filePath = Path.GetFullPath(filePath.Trim());
            }
            catch (Exception ex)
            {
                return "路径格式无效: " + ex.Message;
            }

            string dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir))
                return "无法取得文件所在目录:\r\n" + filePath;

            // ★ 关键改进：父目录缺失则尝试创建（修复 code.ini 首次运行场景）
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); }
                catch (UnauthorizedAccessException ex)
                {
                    return showPathInMessage
                        ? "父目录不存在且无法创建（权限被拒）:\r\n" + dir + "\r\n" + ex.Message
                        : "开通失败：父目录不可用（权限被拒）：\r\n" + dir;
                }
                catch (Exception ex)
                {
                    return showPathInMessage
                        ? "父目录不存在且无法创建:\r\n" + dir + "\r\n" + ex.Message
                        : "开通失败：父目录不可用:\r\n" + dir;
                }
            }

            // 创建文件（如果不存在）
            if (!File.Exists(filePath))
            {
                try
                {
                    // ★ 用 UTF-8 no BOM 写空 ini 占位（ClassIni 兼容 ANSI/UTF-8）
                    File.WriteAllText(filePath, "#ParamSet", new UTF8Encoding(false));
                }
                catch (UnauthorizedAccessException ex)
                {
                    return showPathInMessage
                        ? "创建配置文件失败（权限被拒）:\r\n" + filePath + "\r\n" + ex.Message
                        : "开通失败：无法创建配置文件（权限被拒）。\r\n" + ex.Message;
                }
                catch (IOException ex)
                {
                    return showPathInMessage
                        ? "创建配置文件失败（文件被占用）:\r\n" + filePath + "\r\n" + ex.Message
                        : "开通失败：无法创建配置文件（文件被占用）。\r\n" + ex.Message;
                }
                catch (Exception ex)
                {
                    return showPathInMessage
                        ? "创建配置文件失败:\r\n" + ex.Message
                        : "开通失败：无法创建配置文件。\r\n" + ex.Message;
                }
            }

            // ★ 备份原始 ACL：万一改出问题可以还原
            BackupAcl(filePath);

            // ★ 环境预检：非 ACL 类拦截先查出来，避免白改一遍 ACL
            string preCheck = PreCheckTarget(filePath);
            if (preCheck != null) return preCheck;

            try
            {
                // 0. ★ 2026-09-07：占用检测 —— 文件被独占打开时，改 ACL 也救不了
                ReportProgress(5, 100, "正在检查文件是否被占用...");
                if (IsFileLocked(filePath))
                {
                    return (showPathInMessage
                        ? "文件被其它进程独占，无法开通:\r\n" + filePath
                        : "开通失败：配置文件被其它进程独占。\r\n")
                        + "\r\n请先关闭正在使用该文件的程序（很可能是主软件本身），再重试。";
                }

                // ★ 重试包裹：杀软/Defender 实时扫描会造成瞬时"拒绝访问"，重试往往就过
                Exception lastErr;
                bool done = Retry(() =>
                {
                    ReportProgress(10, 100, "正在修改文件 ACL...");
                    // 1. 去除文件只读属性
                    File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
                    // 2. 对文件应用完整权限
                    ApplyFullAccessToFile(filePath);
                }, 3, out lastErr);

                if (!done)
                {
                    // 失败：还原原始 ACL，绝不留半成品
                    bool restored = RestoreAcl(filePath);
                    RestoreOwners();
                    string hint = EnvironmentProbe.IsPathProtectedByCfa(filePath)
                        ? "\r\n\r提示：目标受 Windows Defender「受控文件夹访问」保护，请把主程序加入允许列表。"
                        : "";
                    return "开通失败（已重试 3 次，原始 ACL 已还原）:\r\n"
                        + (lastErr != null ? lastErr.Message : "") + hint
                        + (restored ? "" : "\r\n★ 注意：原始 ACL 还原失败，请手动检查该文件权限。");
                }

                // ★ P0 修复：父目录只做"最小必要"授权 —— 仅当前用户 / Administrators / SYSTEM，
                // 且不设可继承 ACE，避免 Everyone 完全控制被 Windows 传播到整个 Program Files。
                // 该步骤属辅助性质：失败不影响文件本身开通成功。
                ReportProgress(25, 100, "正在为文件所在目录补充最小写权限...");
                bool dirGranted = true;
                if (Directory.Exists(dir))
                {
                    try { AddMinimalWriteAccessToDirectoryOnly(dir); }
                    catch { dirGranted = false; }
                }

                // 所有权兜底只在必要时发生，这里统一还原为原 Owner（ACL 授权保留）
                RestoreOwners();

                // ★ 2026-09-07：开通后实测真的能写（而不是看 ACL 改了以为成功）
                ReportProgress(55, 100, "ACL 设置完成，正在执行写权限实测...");
                var testResult = TestWriteAccess(filePath);
                if (!testResult.Ok)
                {
                    return (showPathInMessage
                        ? "ACL 已修改，但写入实测失败:\r\n" + filePath
                        : "开通失败：ACL 已修改但实测仍写不进。\r\n")
                        + "\r\n诊断: " + testResult.Message
                        + "\r\n（提示：可能是文件被其它进程独占，或 UAC 虚拟化把写入重定向到 "
                        + "%LOCALAPPDATA%\\VirtualStore\\，请检查目标目录是否真的可写）";
                }

                ReportProgress(100, 100, "开通完成，写测试通过。");
                string dirNote = dirGranted
                    ? ""
                    : "\r\n（提示：所在目录的最小写权限补充失败，若还需在该目录内新建文件，请手动处理。）";
                return showPathInMessage
                    ? "开通成功\r\n已为文件开通完整读写权限（去除只读、清除拒绝规则、多用户授权）:\r\n" + filePath
                      + "\r\n\r\n写测试通过: " + testResult.Message + dirNote
                    : "开通成功\r\n软件配置文件读写权限已彻底设置，可正常使用。\r\n\r\n写测试通过。" + dirNote;
            }
            catch (UnauthorizedAccessException ex)
            {
                return "开通失败（权限被拒）:\r\n" + ex.Message
                    + "\r\n\r多半是文件 Owner 不是当前管理员，"
                    + "请先在管理员 cmd 中执行：\r\ntakeown /F \"" + filePath + "\"";
            }
            catch (Exception ex)
            {
                return "开通失败:\r\n" + ex.Message;
            }
        }

        #endregion

        #region ★ 2026-09-07 新增：诊断 + 写测试

        /// <summary>诊断结果。</summary>
        public class AccessResult
        {
            public AccessIssue Issue { get; set; }
            public string Message { get; set; }
            public bool Ok { get { return Issue == AccessIssue.Ok; } }
        }

        public enum AccessIssue
        {
            Ok = 0,
            PathEmpty,
            NotFound,
            ParentDirNotFound,
            AccessDenied,
            ReadOnly,
            Locked,
            OwnerNotAdmin,
            Unknown
        }

        /// <summary>诊断文件/目录的访问状态（不改任何 ACL，仅做一次轻量写探测）。</summary>
        public static AccessResult Diagnose(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return new AccessResult { Issue = AccessIssue.PathEmpty, Message = "路径为空。" };

            try { path = Path.GetFullPath(path.Trim()); }
            catch (Exception ex)
            {
                return new AccessResult { Issue = AccessIssue.Unknown, Message = "路径无效: " + ex.Message };
            }

            bool isDir;
            try
            {
                if (File.Exists(path)) isDir = false;
                else if (Directory.Exists(path)) isDir = true;
                else
                {
                    // 不存在 —— 可能是要新建的文件
                    string parent = Path.GetDirectoryName(path);
                    if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                        return new AccessResult { Issue = AccessIssue.ParentDirNotFound, Message = "父目录不存在:\r\n" + parent };
                    // 父目录存在，试写测试
                    return TestWriteAccess(path);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return new AccessResult { Issue = AccessIssue.AccessDenied, Message = "列举父目录权限被拒: " + ex.Message };
            }

            if (isDir) return TestDirectoryWriteAccess(path);

            // 文件路径
            try
            {
                var attrs = File.GetAttributes(path);
                if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    return new AccessResult { Issue = AccessIssue.ReadOnly, Message = "文件为只读属性。\r\n" + path };

                return TestWriteAccess(path);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new AccessResult { Issue = AccessIssue.AccessDenied, Message = "读取文件属性权限被拒:\r\n" + ex.Message };
            }
            catch (IOException ex)
            {
                return new AccessResult { Issue = AccessIssue.Locked, Message = "文件被占用:\r\n" + ex.Message };
            }
            catch (Exception ex)
            {
                return new AccessResult { Issue = AccessIssue.Unknown, Message = ex.Message };
            }
        }

        /// <summary>
        /// 实测可写性。★ P1 修复：
        /// - 目标文件**已存在**时，直接验证"目标文件自身"能否拿到写句柄（不再误测父目录）；
        /// - 目标不存在时，才退化为"父目录能否创建文件"的探测（用临时文件，不真的建目标文件）。
        /// </summary>
        public static AccessResult TestWriteAccess(string filePath)
        {
            try
            {
                filePath = Path.GetFullPath(filePath);
                string dir = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(dir))
                    return new AccessResult { Issue = AccessIssue.Unknown, Message = "无法取得文件所在目录" };

                if (File.Exists(filePath))
                    return TestFileWritableCore(filePath);

                if (!Directory.Exists(dir))
                    return new AccessResult { Issue = AccessIssue.ParentDirNotFound, Message = "父目录不存在: " + dir };

                return TestCreateDeleteInDirectory(dir);
            }
            catch (Exception ex)
            {
                return new AccessResult { Issue = AccessIssue.Unknown, Message = "写测试异常: " + ex.Message };
            }
        }

        /// <summary>以"追加零字节"方式打开目标文件的写句柄：能打开即证明可写，且不改动文件内容。</summary>
        private static AccessResult TestFileWritableCore(string filePath)
        {
            try
            {
                using (new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                }
                return new AccessResult { Issue = AccessIssue.Ok, Message = "可写入目标文件自身" };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new AccessResult { Issue = AccessIssue.AccessDenied, Message = "写入目标文件被拒: " + ex.Message };
            }
            catch (IOException ex)
            {
                return new AccessResult { Issue = AccessIssue.Locked, Message = "目标文件被占用: " + ex.Message };
            }
            catch (Exception ex)
            {
                return new AccessResult { Issue = AccessIssue.Unknown, Message = "写测试异常: " + ex.Message };
            }
        }

        /// <summary>在目录中创建-写-读-删一个临时文件（用于目标文件尚不存在的场景）。</summary>
        private static AccessResult TestCreateDeleteInDirectory(string dir)
        {
            string testFile = Path.Combine(dir, ".__grant_test_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(testFile, "test", new UTF8Encoding(false));
                string readBack = File.ReadAllText(testFile);
                File.Delete(testFile);
                if (readBack != "test")
                    return new AccessResult { Issue = AccessIssue.Unknown, Message = "写入内容校验失败" };
                return new AccessResult { Issue = AccessIssue.Ok, Message = "可正常创建-写-读-删除文件" };
            }
            catch (UnauthorizedAccessException ex)
            {
                try { if (File.Exists(testFile)) File.Delete(testFile); } catch { }
                return new AccessResult { Issue = AccessIssue.AccessDenied, Message = "写入被拒: " + ex.Message };
            }
            catch (IOException ex)
            {
                try { if (File.Exists(testFile)) File.Delete(testFile); } catch { }
                return new AccessResult { Issue = AccessIssue.Locked, Message = "文件被占用: " + ex.Message };
            }
            catch (Exception ex)
            {
                try { if (File.Exists(testFile)) File.Delete(testFile); } catch { }
                return new AccessResult { Issue = AccessIssue.Unknown, Message = "写测试异常: " + ex.Message };
            }
        }

        /// <summary>实测：在目录中创建-删除临时子项，验证可写。</summary>
        public static AccessResult TestDirectoryWriteAccess(string dirPath)
        {
            try
            {
                dirPath = Path.GetFullPath(dirPath);
                if (!Directory.Exists(dirPath))
                    return new AccessResult { Issue = AccessIssue.NotFound, Message = "目录不存在" };

                string subDir = Path.Combine(dirPath, ".__grant_test_" + Guid.NewGuid().ToString("N"));
                string testFile = Path.Combine(subDir, "a.tmp");
                try
                {
                    Directory.CreateDirectory(subDir);
                    File.WriteAllText(testFile, "test", new UTF8Encoding(false));
                    File.Delete(testFile);
                    Directory.Delete(subDir);
                    return new AccessResult { Issue = AccessIssue.Ok, Message = "可在目录中创建/删除子项" };
                }
                catch (UnauthorizedAccessException ex)
                {
                    try { if (File.Exists(testFile)) File.Delete(testFile); } catch { }
                    try { if (Directory.Exists(subDir)) Directory.Delete(subDir); } catch { }
                    return new AccessResult { Issue = AccessIssue.AccessDenied, Message = "目录写入被拒: " + ex.Message };
                }
                catch (IOException ex)
                {
                    try { if (File.Exists(testFile)) File.Delete(testFile); } catch { }
                    try { if (Directory.Exists(subDir)) Directory.Delete(subDir); } catch { }
                    return new AccessResult { Issue = AccessIssue.Locked, Message = "目录被占用: " + ex.Message };
                }
            }
            catch (Exception ex)
            {
                return new AccessResult { Issue = AccessIssue.Unknown, Message = "写测试异常: " + ex.Message };
            }
        }

        /// <summary>
        /// ★ 2026-09-07：开通前的环境预检。这几类都不是 ACL 问题，
        /// 不先查出来就硬改 ACL 属于白改 —— 直接返回处置建议。
        /// 返回 null 表示无致命拦截，可以继续开通。
        /// </summary>
        public static string PreCheckTarget(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // EFS 加密：证书问题，改 ACL 无用
            if (EnvironmentProbe.IsEfsEncrypted(path))
                return "★目标被 EFS 加密，属证书问题而非权限问题，开通权限无法解决：\r\n" + path
                    + "\r\n\r\n请改用当初加密该文件的账户登录，或先解密该文件。";

            // 网络路径：权限在远端服务器上
            if (EnvironmentProbe.IsNetworkPath(path))
                return "★目标是网络路径，权限由远端服务器决定：\r\n" + path
                    + "\r\n\r\n请到文件服务器上为相应用户/计算机账户授予写权限。";

            // 磁盘只读 / 满 / 未就绪
            string diskProblem = EnvironmentProbe.GetDiskProblem(path);
            if (diskProblem != null)
                return "★磁盘异常，无法开通：\r\n" + diskProblem;

            // 路径超长
            if (EnvironmentProbe.IsPathTooLong(path))
                return "★路径过长（超过 248 字符），可能触发 MAX_PATH 限制：\r\n" + path;

            return null;
        }

        #endregion

        #region ACL 应用

        #region ACL 备份 / 回滚 / 重试

        /// <summary>本次会话备份的 ACL（SDDL），用于失败时还原。"不容有失"的保险丝。</summary>
        private static readonly Dictionary<string, string> _aclBackup =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _backupLock = new object();

        /// <summary>
        /// 本次会话中被夺取过所有者的路径 → 原 Owner SID。
        /// 只有在"改不动 ACL 才夺所有权"的兜底路径里才会记录；成功后统一还原，避免永久改变系统文件归属。
        /// </summary>
        private static readonly Dictionary<string, string> _ownerBackup =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>开通前备份目标（文件+目录）的原始 ACL。</summary>
        public static void BackupAcl(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string sddl = null;
                if (File.Exists(path))
                    sddl = new FileInfo(path).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.All);
                else if (Directory.Exists(path))
                    sddl = new DirectoryInfo(path).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.All);

                if (sddl != null)
                {
                    lock (_backupLock) _aclBackup[path] = sddl;
                }
            }
            catch { }
        }

        /// <summary>还原目标的原始 ACL（开通失败时调用）。返回是否成功。</summary>
        public static bool RestoreAcl(string path)
        {
            string sddl;
            lock (_backupLock)
            {
                if (!_aclBackup.TryGetValue(path, out sddl)) return false;
            }
            try
            {
                if (File.Exists(path))
                {
                    var acl = new FileSecurity();
                    acl.SetSecurityDescriptorSddlForm(sddl);
                    new FileInfo(path).SetAccessControl(acl);
                }
                else if (Directory.Exists(path))
                {
                    var acl = new DirectorySecurity();
                    acl.SetSecurityDescriptorSddlForm(sddl);
                    new DirectoryInfo(path).SetAccessControl(acl);
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>夺取所有权前记录原 Owner（同一路径只记录第一次，即真正的原始值）。</summary>
        private static void BackupOwner(string path)
        {
            try
            {
                SecurityIdentifier sid = null;
                if (File.Exists(path))
                    sid = new FileInfo(path).GetAccessControl(AccessControlSections.Owner)
                        .GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;
                else if (Directory.Exists(path))
                    sid = new DirectoryInfo(path).GetAccessControl(AccessControlSections.Owner)
                        .GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;

                if (sid != null)
                {
                    lock (_backupLock)
                    {
                        if (!_ownerBackup.ContainsKey(path)) _ownerBackup[path] = sid.Value;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 把本次会话中夺取过的所有者还原为原 Owner（尽力而为；ACL 授权保持不变）。
        /// 用于"成功也需要还原"的场景，避免永久改变系统文件归属。
        /// </summary>
        public static void RestoreOwners()
        {
            List<KeyValuePair<string, string>> items;
            lock (_backupLock)
            {
                items = new List<KeyValuePair<string, string>>(_ownerBackup);
                _ownerBackup.Clear();
            }

            foreach (var kv in items)
            {
                try
                {
                    var sid = new SecurityIdentifier(kv.Value);
                    if (File.Exists(kv.Key))
                    {
                        var fi = new FileInfo(kv.Key);
                        FileSecurity acl = fi.GetAccessControl(AccessControlSections.Owner);
                        acl.SetOwner(sid);
                        fi.SetAccessControl(acl);
                    }
                    else if (Directory.Exists(kv.Key))
                    {
                        var di = new DirectoryInfo(kv.Key);
                        DirectorySecurity acl = di.GetAccessControl(AccessControlSections.Owner);
                        acl.SetOwner(sid);
                        di.SetAccessControl(acl);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// ★ 重试执行。杀软实时扫描、 Defender 后台扫描会造成"瞬时拒绝访问"，
        ///   重试 2~3 次往往就成功了。返回值：成功为 true。
        /// </summary>
        private static bool Retry(Action action, int times, out Exception lastError)
        {
            lastError = null;
            for (int i = 0; i < times; i++)
            {
                try { action(); return true; }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (i < times - 1) System.Threading.Thread.Sleep(150 * (i + 1));
                }
            }
            return false;
        }

        #endregion

        // 特权只需启用一次
        private static bool _privilegesTried;
        private static string _privilegeDetail = "";
        private static readonly object _privilegeLock = new object();

        /// <summary>启用提权（SeTakeOwnership 等），幂等。</summary>
        public static string EnsurePrivileges()
        {
            lock (_privilegeLock)
            {
                if (!_privilegesTried)
                {
                    _privilegesTried = true;
                    ReportProgress(2, 100, "正在启用管理员特权...");
                    try { PrivilegeHelper.EnableAllPrivileges(out _privilegeDetail); }
                    catch (Exception ex) { _privilegeDetail = "提权异常: " + ex.Message; }
                }
                return _privilegeDetail;
            }
        }

        /// <summary>
        /// ★ 夺取文件所有权：把 Owner 设为 Administrators 组。
        /// 这是"管理员也改不动 ACL"的根因解法 —— 管理员 ≠ 所有者。
        /// 失败时不抛异常（交由上层继续尝试改 ACL，可能本来就有权限）。
        /// </summary>
        private static void TakeOwnershipFile(FileInfo fileInfo)
        {
            EnsurePrivileges();
            BackupOwner(fileInfo.FullName);
            try
            {
                FileSecurity acl = fileInfo.GetAccessControl();
                acl.SetOwner(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
                fileInfo.SetAccessControl(acl);
            }
            catch { }
        }

        /// <summary>★ 夺取目录所有权（同上）。</summary>
        private static void TakeOwnershipDir(DirectoryInfo dirInfo)
        {
            EnsurePrivileges();
            BackupOwner(dirInfo.FullName);
            try
            {
                DirectorySecurity acl = dirInfo.GetAccessControl();
                acl.SetOwner(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
                dirInfo.SetAccessControl(acl);
            }
            catch { }
        }

        /// <summary>
        /// ★ 2026-09-07：检测文件是否被其它进程独占打开。
        /// 被占用时改 ACL 也没用 —— 必须先关闭占用的程序（通常是主软件本身）。
        /// </summary>
        public static bool IsFileLocked(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
            System.IO.FileStream fs = null;
            try
            {
                fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open,
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
                return false;
            }
            catch (System.IO.IOException) { return true; }
            catch (UnauthorizedAccessException) { return false; }  // 权限问题不是占用
            finally { if (fs != null) try { fs.Dispose(); } catch { } }
        }

        /// <summary>
        /// 对文件应用最彻底的权限：清除 Deny 规则 → 给多个身份授予 FullControl。
        /// ★ P1：优先直接改 ACL；只有确实改不动（Owner 非管理员）时才夺取所有权再试，
        /// 尽量不永久改变系统文件的所有者归属。
        /// </summary>
        private static void ApplyFullAccessToFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            try
            {
                SetFileAclCore(fileInfo);
            }
            catch (UnauthorizedAccessException)
            {
                // 含 PrivilegeNotHeldException（SACL 特权未启用）：夺所有权再试一次
                TakeOwnershipFile(fileInfo);
                SetFileAclCore(fileInfo);
            }
        }

        private static void SetFileAclCore(FileInfo fileInfo)
        {
            FileSecurity acl = fileInfo.GetAccessControl(AccessControlSections.All);

            // --- 清除所有 Deny 规则 ---
            RemoveDenyRules(acl.GetAccessRules(true, true, typeof(SecurityIdentifier)), acl);

            // --- 为多个身份授予 FullControl（using 确保 WindowsIdentity 句柄释放） ---
            using (var identity = WindowsIdentity.GetCurrent())
            {
                AddFullControlAllow(acl, identity.User);
                if (identity.Groups != null)
                {
                    foreach (var groupSid in identity.Groups)
                    {
                        try { AddFullControlAllow(acl, groupSid); } catch { }
                    }
                }
            }
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null));
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null));
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.WorldSid, null));
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null));
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));

            fileInfo.SetAccessControl(acl);
        }

        private static void RemoveDenyRules(AuthorizationRuleCollection rules, FileSystemSecurity acl)
        {
            var denyRules = new List<FileSystemAccessRule>();
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.AccessControlType == AccessControlType.Deny)
                    denyRules.Add(rule);
            }
            foreach (var rule in denyRules)
                acl.RemoveAccessRule(rule);
        }

        /// <summary>
        /// 对目录自身应用"彻底"权限：清除 Deny → 多身份授予 FullControl + 完整继承。
        /// ★ P1：优先直接改 ACL，改不动才夺所有权。注意调用方（GrantDirectoryAccess）已做过目录自身
        /// 的 ACL 设置，本方法用于子目录，不要与入口重复调用。
        /// </summary>
        private static void ApplyFullAccessToDirectoryOnly(string dirPath)
        {
            var dirInfo = new DirectoryInfo(dirPath);
            try
            {
                SetDirectoryAclCore(dirInfo);
            }
            catch (UnauthorizedAccessException)
            {
                TakeOwnershipDir(dirInfo);
                SetDirectoryAclCore(dirInfo);
            }
        }

        /// <summary>目录树中的子目录（与 ApplyFullAccessToDirectoryOnly 行为一致）。</summary>
        private static void ApplyFullAccessToDirectoryDeep(string dirPath)
        {
            ApplyFullAccessToDirectoryOnly(dirPath);
        }

        private static void SetDirectoryAclCore(DirectoryInfo dirInfo)
        {
            DirectorySecurity acl = dirInfo.GetAccessControl(AccessControlSections.All);

            RemoveDenyRules(acl.GetAccessRules(true, true, typeof(SecurityIdentifier)), acl);

            var inheritFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            var propagFlags = PropagationFlags.None;

            using (var identity = WindowsIdentity.GetCurrent())
                AddFullControlAllowToDir(acl, identity.User, inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.WorldSid, null), inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), inheritFlags, propagFlags);

            dirInfo.SetAccessControl(acl);
        }

        /// <summary>
        /// ★ P0 修复：为"文件模式的父目录"（如 C:\Program Files）添加**最小必要**的写权限。
        /// 只授权 当前用户 + Administrators + SYSTEM，且使用**不可继承**的 ACE ——
        /// 避免可继承 ACE 被 Windows 自动传播到整个子目录树（等于给 Program Files 全员完全控制）。
        /// 不夺所有权、不清除 Deny 规则。
        /// </summary>
        private static void AddMinimalWriteAccessToDirectoryOnly(string dirPath)
        {
            var dirInfo = new DirectoryInfo(dirPath);
            DirectorySecurity acl = dirInfo.GetAccessControl(AccessControlSections.Access);

            using (var identity = WindowsIdentity.GetCurrent())
                AddFullControlAllowToDir(acl, identity.User, InheritanceFlags.None, PropagationFlags.None);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                InheritanceFlags.None, PropagationFlags.None);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                InheritanceFlags.None, PropagationFlags.None);

            dirInfo.SetAccessControl(acl);
        }

        /// <summary>
        /// 目录树开通（调用前须已开通目录自身）：逐层容错枚举全部子目录/文件，逐个开通并上报进度。
        /// 返回处理失败的项数。
        /// ★ P0 修复：改为逐层枚举 —— 旧版用 AllDirectories 一次性枚举，只要有一个子目录无权限就
        /// 整棵树枚举失败、被 catch 吞掉，最终静默报"开通成功"。
        /// </summary>
        private static int ApplyFullAccessToDirectoryTree(string dirPath)
        {
            var dirs = new List<string>();
            var files = new List<string>();
            CollectChildrenTolerant(dirPath, dirs, files);

            int total = dirs.Count + files.Count;
            if (total == 0)
            {
                ReportProgress(100, 100, "目录内没有子项，已开通目录自身。");
                return 0;
            }

            // 目录自身已完成，从 10% 起步，随后按"已处理项数 / 总项数"真实递增
            ReportProgress(10, 100, "共需开通 " + total + " 个子项...");
            int current = 0;
            int failed = 0;
            foreach (string item in Concat(dirs, files))
            {
                try
                {
                    if (Directory.Exists(item))
                    {
                        ApplyFullAccessToDirectoryDeep(item);
                    }
                    else
                    {
                        try { File.SetAttributes(item, File.GetAttributes(item) & ~FileAttributes.ReadOnly); } catch { }
                        ApplyFullAccessToFile(item);
                    }
                }
                catch { failed++; }

                current++;
                // 项进度映射到 10%~100%，每处理 8 项刷新一次避免高频闪烁
                if ((current & 7) == 0 || current == total)
                {
                    int percent = 10 + (int)((long)current * 90 / total);
                    ReportProgress(percent, 100, "正在开通: " + Path.GetFileName(item));
                }
            }
            return failed;
        }

        /// <summary>
        /// 逐层（而非一次性 AllDirectories）容错枚举：某个子目录无权限只跳过它自身，不影响其它分支。
        /// </summary>
        private static void CollectChildrenTolerant(string root, List<string> dirs, List<string> files)
        {
            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                string cur = stack.Pop();

                try
                {
                    foreach (string f in Directory.GetFiles(cur))
                        files.Add(f);
                }
                catch { }

                try
                {
                    foreach (string d in Directory.GetDirectories(cur))
                    {
                        dirs.Add(d);
                        stack.Push(d);
                    }
                }
                catch { }
            }
        }

        private static IEnumerable<string> Concat(List<string> first, List<string> second)
        {
            foreach (string s in first) yield return s;
            foreach (string s in second) yield return s;
        }

        private static void AddFullControlAllow(FileSecurity acl, IdentityReference sid)
        {
            if (sid == null) return;
            acl.AddAccessRule(new FileSystemAccessRule(
                sid,
                FileSystemRights.FullControl,
                AccessControlType.Allow));
        }

        private static void AddFullControlAllowToDir(DirectorySecurity acl, IdentityReference sid,
            InheritanceFlags inheritFlags, PropagationFlags propagFlags)
        {
            if (sid == null) return;
            acl.AddAccessRule(new FileSystemAccessRule(
                sid,
                FileSystemRights.FullControl,
                inheritFlags,
                propagFlags,
                AccessControlType.Allow));
        }

        #endregion
    }
}