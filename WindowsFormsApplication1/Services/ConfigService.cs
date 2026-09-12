using System;
using demo;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 配置服务。收口原 Form1 中散落的 <see cref="ClassIni"/> 调用（读写 code.ini 等配置文件）。
    /// <para>
    /// 内部委托既有的 <see cref="ClassIni"/> 实现，保持 INI 读写行为完全不变；
    /// Form1 只通过本服务读写配置，不再直接持有 <see cref="ClassIni"/> 实例，便于后续统一配置来源与单测。
    /// </para>
    /// </summary>
    public sealed class ConfigService : IDisposable
    {
        private readonly ClassIni _ini = new ClassIni();

        /// <summary>★ N4：显式刷新 INI 缓存并释放（由组合根在退出时调用）。</summary>
        public void Dispose()
        {
            _ini.Dispose();
        }

        /// <summary>加载指定 INI 文件（AFileName 为完整路径）。</summary>
        public void ReadINIFile(string AFileName)
        {
            _ini.ReadINIFile(AFileName);
        }

        /// <summary>读取字符串配置项；缺省返回 Default。</summary>
        public string ReadString(string Section, string Ident, string Default)
        {
            return _ini.ReadString(Section, Ident, Default);
        }

        /// <summary>写入字符串配置项。</summary>
        public void WriteString(string Section, string Ident, string Value)
        {
            _ini.WriteString(Section, Ident, Value);
        }
    }
}
