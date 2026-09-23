using System;
using System.IO;
using demo;
using WindowsFormsApplication1.Core.Logging;

namespace WindowsFormsApplication1.Configuration
{
    /// <summary>
    /// 基于 <see cref="demo.ClassIni"/> 的配置中心实现。
    /// <para>
    /// Menu.ini 首行存放 VisionPro 作业（.vpp）绝对路径；其余参数沿用 INI 的「节/键」模型，
    /// 由本类提供类型安全的读写封装。所有异常均向调用方透出或由默认值兜底，不吞掉错误。
    /// </para>
    /// </summary>
    public sealed class AppConfiguration : IAppConfiguration
    {
        private readonly ClassIni _ini = new ClassIni();
        private readonly string _iniPath;

        public AppConfiguration(string iniPath = null)
        {
            _iniPath = iniPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Menu.ini");
            _ini.ReadINIFile(_iniPath);
        }

        public string VisionJobPath
        {
            get
            {
                try
                {
                    string[] lines = File.ReadAllLines(_iniPath);
                    if (lines.Length > 0) return lines[0].Trim();
                }
                catch (Exception ex)
                {
                    Log.Error("读取 VisionJobPath 失败: " + ex.Message);
                }
                return string.Empty;
            }
        }

        public void SetVisionJobPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                // ★修复：原实现 File.WriteAllText 整文件覆盖——Menu.ini 首行以外的所有节/键
                // 会被全部抹掉(与 VisionJobPath 读取"仅首行"的约定对应，写也必须只改首行)。
                string[] lines = File.Exists(_iniPath) ? File.ReadAllLines(_iniPath) : new string[0];
                if (lines.Length == 0)
                {
                    File.WriteAllText(_iniPath, path.Trim());
                }
                else
                {
                    lines[0] = path.Trim();
                    File.WriteAllLines(_iniPath, lines);
                }
                ClassIni.ClearCache(_iniPath);   // 绕开 ClassIni 直接改文件，须作废其读缓存
            }
            catch (Exception ex) { Log.Error("写入 VisionJobPath 失败: " + ex.Message); }
        }

        public string Read(string section, string key, string defaultValue)
        {
            return _ini.ReadString(section, key, defaultValue ?? string.Empty);
        }

        public int ReadInt(string section, string key, int defaultValue)
        {
            return _ini.ReadInteger(section, key, defaultValue);
        }

        public double ReadDouble(string section, string key, double defaultValue)
        {
            return _ini.ReadDouble(section, key, defaultValue);
        }

        public bool ReadBool(string section, string key, bool defaultValue)
        {
            return _ini.ReadBool(section, key, defaultValue);
        }

        public void Write(string section, string key, string value)
        {
            _ini.WriteString(section, key, value ?? string.Empty);
        }
    }
}
