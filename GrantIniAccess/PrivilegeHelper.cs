using System;
using System.Runtime.InteropServices;

namespace GrantIniAccess
{
    /// <summary>
    /// 提权辅助：启用 SeTakeOwnershipPrivilege / SeBackupPrivilege / SeRestorePrivilege 等特权。
    ///
    /// ★ 为什么必须有这个：
    /// 在 Program Files 等受保护目录下，文件 Owner 常常是 TrustedInstaller 或另一个账户。
    /// 此时即便进程已经以管理员运行，调用 SetOwner / SetAccessControl 依然会抛
    /// UnauthorizedAccessException —— 因为"管理员"不等于"所有者"。
    /// 只有先启用 SeTakeOwnershipPrivilege 才能夺取所有权，进而改 ACL。
    /// 这是做到"尽可能开通成功"的关键一步，缺了它就只能让用户手敲 takeown 命令。
    /// </summary>
    public static class PrivilegeHelper
    {
        #region P/Invoke

        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(
            IntPtr tokenHandle,
            bool disableAllPrivileges,
            ref TOKEN_PRIVILEGES newState,
            uint bufferLength,
            IntPtr previousState,
            IntPtr returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID Luid;
            public uint Attributes;
        }

        #endregion

        /// <summary>需要启用的特权列表。</summary>
        private static readonly string[] RequiredPrivileges = new[]
        {
            "SeTakeOwnershipPrivilege",   // 夺取所有权（最关键）
            "SeBackupPrivilege",          // 绕过 ACL 读取
            "SeRestorePrivilege",         // 绕过 ACL 写入/设置所有者
            "SeSecurityPrivilege",        // 读取/设置 SACL
            "SeChangeNotifyPrivilege"     // 遍历目录
        };

        /// <summary>
        /// 尝试启用全部需要的特权。返回实际成功启用的特权数量。
        /// 部分失败不抛异常（例如某些精简系统/组策略禁用了个别特权），
        /// 但只要 SeTakeOwnershipPrivilege 成功，绝大多数场景就能拿下。
        /// </summary>
        public static int EnableAllPrivileges(out string detail)
        {
            detail = "";
            IntPtr token;
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, out token) || token == IntPtr.Zero)
            {
                detail = "无法打开进程令牌（OpenProcessToken 失败）";
                return 0;
            }

            int ok = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            try
            {
                foreach (string priv in RequiredPrivileges)
                {
                    LUID luid;
                    if (!LookupPrivilegeValue(null, priv, out luid))
                    {
                        sb.AppendLine(priv + ": 系统不支持");
                        continue;
                    }

                    TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
                    tp.PrivilegeCount = 1;
                    tp.Luid = luid;
                    tp.Attributes = SE_PRIVILEGE_ENABLED;

                    bool enabled = AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                    int err = Marshal.GetLastWin32Error();
                    if (enabled && err == 0)
                    {
                        ok++;
                        sb.AppendLine(priv + ": 已启用");
                    }
                    else
                    {
                        sb.AppendLine(priv + ": 启用失败(错误 " + err + ")");
                    }
                }
            }
            finally
            {
                CloseHandle(token);
            }

            detail = sb.ToString();
            return ok;
        }
    }
}