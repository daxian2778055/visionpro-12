// 版本标识（手工维护的固定值，不随时间/构建自动变化）
// 规则：每次推送前由维护者更新为 “推送日期 + 本次推送的提交短哈希”（格式 yyyy.MM.dd+shortHash）。
// 版本信息对话框（Form1.Camera58Events.cs 版本信息ToolStripMenuItem_Click）读取本类显示。
namespace WindowsFormsApplication1
{
    internal static class BuildInfo
    {
        public const string Version = "2026.09.25+513102a";
        public const string BuildTime = "2026-09-25 11:53:45";
    }
}
