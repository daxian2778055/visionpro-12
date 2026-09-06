using System;

namespace WindowsFormsApplication1.Configuration
{
    /// <summary>
    /// 应用配置抽象。集中管理 Menu.ini（视觉作业路径等）与通用键值读写，
    /// 业务代码通过本接口访问，避免各处硬编码 INI 路径、节名与字符串键。
    /// </summary>
    public interface IAppConfiguration
    {
        /// <summary>VisionPro 作业文件（.vpp）绝对路径，来自 Menu.ini 首行。</summary>
        string VisionJobPath { get; }

        /// <summary>更新 VisionPro 作业路径（写回 Menu.ini 首行）。</summary>
        void SetVisionJobPath(string path);

        string Read(string section, string key, string defaultValue);
        int ReadInt(string section, string key, int defaultValue);
        double ReadDouble(string section, string key, double defaultValue);
        bool ReadBool(string section, string key, bool defaultValue);
        void Write(string section, string key, string value);
    }
}
