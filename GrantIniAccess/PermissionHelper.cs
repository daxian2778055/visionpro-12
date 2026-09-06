using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace GrantIniAccess
{
    public static class PermissionHelper
    {
        private const string InternalConfigFilePath = @"C:\Program Files\test.ini";

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

        /// <summary>文件开通：使用内置路径。</summary>
        public static string GrantDefaultFileAccess()
        {
            return GrantConfigFileAccess(InternalConfigFilePath, showPathInMessage: false);
        }

        private static string GrantConfigFileAccess(string filePath, bool showPathInMessage)
        {
            filePath = Path.GetFullPath(filePath.Trim());
            string dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return showPathInMessage
                    ? "目录不存在:\r\n" + dir
                    : "开通失败：目标目录不可用，请确认系统环境正常。";

            // 创建文件（如果不存在）
            if (!File.Exists(filePath))
            {
                try { File.WriteAllText(filePath, "#ParamSet", Encoding.Default); }
                catch (Exception ex)
                {
                    return showPathInMessage
                        ? "创建配置文件失败:\r\n" + ex.Message
                        : "开通失败：无法创建配置文件。\r\n" + ex.Message;
                }
            }

            try
            {
                // 1. 去除文件只读属性
                File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
                // 2. 对文件应用完整权限
                ApplyFullAccessToFile(filePath);
                // 3. 对所在目录也开通
                if (Directory.Exists(dir))
                    ApplyFullAccessToDirectory(dir);

                if (showPathInMessage)
                    return "开通成功\r\n已为文件开通完整读写权限（去除只读、清除拒绝规则、多用户授权）:\r\n" + filePath;
                return "开通成功\r\n软件配置文件读写权限已彻底设置，可正常使用。";
            }
            catch (Exception ex)
            {
                return "开通失败:\r\n" + ex.Message;
            }
        }

        public static string GrantDirectoryAccess(string dirPath)
        {
            if (string.IsNullOrWhiteSpace(dirPath))
                return "目录路径不能为空。";

            dirPath = Path.GetFullPath(dirPath.Trim());
            if (!Directory.Exists(dirPath))
                return "目录不存在:\r\n" + dirPath;

            try
            {
                ApplyFullAccessToDirectory(dirPath);
                return "开通成功（指定目录）\r\n已为目录开通完整读写权限（含子项继承）:\r\n" + dirPath;
            }
            catch (Exception ex)
            {
                return "开通失败:\r\n" + ex.Message;
            }
        }

        /// <summary>
        /// 对文件应用最彻底的权限：清除所有 Deny 规则，给多个身份授予 FullControl。
        /// </summary>
        private static void ApplyFullAccessToFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            FileSecurity acl = fileInfo.GetAccessControl(AccessControlSections.All);

            // --- 第一步：清除所有 Deny 规则 ---
            AuthorizationRuleCollection rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
            var denyRulesToRemove = new System.Collections.Generic.List<FileSystemAccessRule>();
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.AccessControlType == AccessControlType.Deny)
                    denyRulesToRemove.Add(rule);
            }
            foreach (var rule in denyRulesToRemove)
                acl.RemoveAccessRule(rule);

            // --- 第二步：为多个身份授予 FullControl ---
            // 当前用户
            AddFullControlAllow(acl, WindowsIdentity.GetCurrent().User);
            // 当前用户主组
            if (WindowsIdentity.GetCurrent().Groups != null)
            {
                foreach (var groupSid in WindowsIdentity.GetCurrent().Groups)
                {
                    try { AddFullControlAllow(acl, groupSid); } catch { }
                }
            }
            // Users 组
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null));
            // Authenticated Users
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null));
            // Everyone
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.WorldSid, null));
            // SYSTEM
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null));
            // Administrators
            AddFullControlAllow(acl, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));

            // --- 第三步：禁用继承（可选：阻止父目录拒绝规则继续影响此文件），然后保留已转换的规则 ---
            // 注意：这里不禁用继承，因为大多数情况下继承是需要的。
            // 如果需要禁用，取消下面两行注释即可：
            // acl.SetAccessRuleProtection(true, true);

            fileInfo.SetAccessControl(acl);
        }

        /// <summary>
        /// 对目录应用最彻底的权限：清除所有 Deny 规则，给多个身份授予 FullControl + 完整继承。
        /// </summary>
        private static void ApplyFullAccessToDirectory(string dirPath)
        {
            var dirInfo = new DirectoryInfo(dirPath);
            DirectorySecurity acl = dirInfo.GetAccessControl(AccessControlSections.All);

            // --- 第一步：清除所有 Deny 规则 ---
            AuthorizationRuleCollection rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
            var denyRulesToRemove = new System.Collections.Generic.List<FileSystemAccessRule>();
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.AccessControlType == AccessControlType.Deny)
                    denyRulesToRemove.Add(rule);
            }
            foreach (var rule in denyRulesToRemove)
                acl.RemoveAccessRule(rule);

            // --- 第二步：为多个身份授予 FullControl + 完整继承 ---
            var inheritFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            var propagFlags = PropagationFlags.None;

            // 当前用户
            AddFullControlAllowToDir(acl, WindowsIdentity.GetCurrent().User, inheritFlags, propagFlags);
            // Users 组
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), inheritFlags, propagFlags);
            // Authenticated Users
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), inheritFlags, propagFlags);
            // Everyone
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.WorldSid, null), inheritFlags, propagFlags);
            // SYSTEM
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), inheritFlags, propagFlags);
            // Administrators
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), inheritFlags, propagFlags);

            dirInfo.SetAccessControl(acl);

            // --- 第三步：递归处理子目录和文件 ---
            try
            {
                foreach (string subDir in Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        ApplyFullAccessToDirectoryDeep(subDir);
                    }
                    catch { }
                }
                foreach (string file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                        ApplyFullAccessToFile(file);
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>对子目录深度应用权限（含清除 Deny + 多身份 FullControl）。</summary>
        private static void ApplyFullAccessToDirectoryDeep(string dirPath)
        {
            var dirInfo = new DirectoryInfo(dirPath);
            DirectorySecurity acl = dirInfo.GetAccessControl(AccessControlSections.All);

            // 清除 Deny
            AuthorizationRuleCollection rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.AccessControlType == AccessControlType.Deny)
                    acl.RemoveAccessRule(rule);
            }

            var inheritFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            var propagFlags = PropagationFlags.None;

            AddFullControlAllowToDir(acl, WindowsIdentity.GetCurrent().User, inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.WorldSid, null), inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), inheritFlags, propagFlags);
            AddFullControlAllowToDir(acl, new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), inheritFlags, propagFlags);

            dirInfo.SetAccessControl(acl);
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
    }
}
