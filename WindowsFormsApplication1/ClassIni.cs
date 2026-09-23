using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace demo
{
    public class ClassIni : IDisposable
    {
        public string FileName; //INI文件名

        private bool _disposed;

        //声明读写INI文件的API函数
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);

        // ── P7：内存读缓存 ──────────────────────────────────────────────
        // 读 INI 走 kernel32 P/Invoke，频繁读取（尤其保存相机绑定时成百上千次）
        // 会造成明显卡顿。这里按"规范化全路径"维护一份进程内读缓存，所有指向同一
        // 文件的 ClassIni 实例共享；任意实例的写/删/清段都会同步失效对应条目。
        //       外部修改(其它进程/手工编辑)由读入口的 mtime 比对自动整文件失效，
        //       也可调用 ClearCache() 主动作废。
        private static readonly object _cacheLock = new object();
        private static readonly Dictionary<string, FileCache> _fileCaches =
            new Dictionary<string, FileCache>(StringComparer.OrdinalIgnoreCase);

        private sealed class FileCache
        {
            // (section\0ident) 小写键 -> 已 Trim 的值
            public readonly Dictionary<string, string> Values =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // section -> 该段所有 ident（已 Trim）
            public readonly Dictionary<string, List<string>> SectionIdents =
                new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            // 该文件全部 section 名（已 Trim）；null 表示未缓存
            public List<string> AllSections;
            // ★文件最后写入时间快照：外部(其它进程/手工编辑)改动后自动失效整份缓存；MinValue=尚未快照
            public DateTime MtimeUtc = DateTime.MinValue;
        }

        private static string CacheKey(string section, string ident) => section + "\0" + ident;

        private static FileCache GetCache(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            string key = Path.GetFullPath(fileName);
            lock (_cacheLock)
            {
                if (!_fileCaches.TryGetValue(key, out var fc))
                {
                    fc = new FileCache();
                    _fileCaches[key] = fc;
                }
                return fc;
            }
        }

        private static void InvalidateSection(FileCache fc, string section)
        {
            if (fc == null) return;
            // ★第26轮#16：读写入口均在 _cacheLock 内访问这三个字典，本方法原先无锁枚举+删除，
            //   与并发读交错会损坏 Dictionary 内部结构（丢条目甚至死循环）。monitor 可重入，
            //   已持锁的外层调用方无影响。
            lock (_cacheLock)
            {
                string prefix = section + "\0";
                var toRemove = new List<string>();
                foreach (var k in fc.Values.Keys)
                    if (k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        toRemove.Add(k);
                foreach (var k in toRemove) fc.Values.Remove(k);
                fc.SectionIdents.Remove(section);
                fc.AllSections = null;
            }
        }

        // ★ 缓存按文件写入时间(mtime)失效：原前提"本进程是唯一写者"不成立时(其它进程/
        //   资源管理器手工编辑 test.ini)，读缓存会永久读到陈旧值。每次读入口比对一次
        //   GetLastWriteTimeUtc(一次 stat，远便宜于 P/Invoke 解析)，变化即整文件作废。
        private void ValidateCacheAgainstFile()
        {
            var fc = GetCache(FileName);
            if (fc == null) return;
            DateTime mtime;
            try { mtime = File.GetLastWriteTimeUtc(FileName); }
            catch { return; }
            lock (_cacheLock)
            {
                if (fc.MtimeUtc == DateTime.MinValue) { fc.MtimeUtc = mtime; return; }
                if (fc.MtimeUtc != mtime)
                {
                    fc.Values.Clear();
                    fc.SectionIdents.Clear();
                    fc.AllSections = null;
                    fc.MtimeUtc = mtime;
                }
            }
        }

        // 本进程写入后刷新 mtime 快照，防止自家写触发误失效
        private void TouchCacheMtime(FileCache fc)
        {
            if (fc == null) return;
            try
            {
                DateTime mtime = File.GetLastWriteTimeUtc(FileName);
                lock (_cacheLock) fc.MtimeUtc = mtime;
            }
            catch { }
        }

        //类的构造函数，传递INI文件名
        public void ReadINIFile(string AFileName)
        {
            if (string.IsNullOrEmpty(AFileName))
                throw new ArgumentException("INI文件路径不能为空");

            // 判断文件是否存在
            FileInfo fileInfo = new FileInfo(AFileName);
            if (!fileInfo.Exists)
            {
                //文件不存在，建立文件
                try
                {
                    using (StreamWriter sw = new StreamWriter(AFileName, false, Encoding.Default))
                    {
                        sw.Write("#ParamSet");
                        sw.Flush();
                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("Ini文件不存在且创建失败: " + ex.Message, ex);
                }
            }
            //必须是完全路径，不能是相对路径
            FileName = fileInfo.FullName;
        }

        //写INI文件
        public void WriteString(string Section, string Ident, string Value)
        {
            if (string.IsNullOrEmpty(FileName))
                throw new InvalidOperationException("INI文件未初始化");
            if (string.IsNullOrEmpty(Section))
                throw new ArgumentException("Section不能为空");

            if (!WritePrivateProfileString(Section, Ident, Value, FileName))
            {
                throw new ApplicationException("写Ini文件出错，Section=" + Section + ", Ident=" + Ident);
            }

            // P7：写成功后同步缓存，保证同进程内后续读立即看到新值
            var fc = GetCache(FileName);
            if (fc != null && !string.IsNullOrEmpty(Ident))
            {
                string ck = CacheKey(Section, Ident);
                lock (_cacheLock)
                {
                    fc.Values[ck] = (Value ?? string.Empty).Trim();
                    fc.SectionIdents.Remove(Section); // ident 集合可能新增/变化
                    fc.AllSections = null;            // 段集合可能新增
                }
                TouchCacheMtime(fc);   // ★自家写：刷新 mtime，避免下次读误判为外部改动
            }
        }

        //读取INI文件指定
        public string ReadString(string Section, string Ident, string Default)
        {
            if (string.IsNullOrEmpty(FileName))
                return Default;

            ValidateCacheAgainstFile();   // ★外部改动检测(mtime)，变化则整文件缓存作废

            // 整段/整文件查询（section 或 ident 为空）无法走单键缓存，直接 P/Invoke
            if (string.IsNullOrEmpty(Section) || string.IsNullOrEmpty(Ident))
                return RawReadString(Section, Ident, Default);

            var fc = GetCache(FileName);
            string ck = CacheKey(Section, Ident);
            lock (_cacheLock)
            {
                if (fc != null && fc.Values.TryGetValue(ck, out var cached))
                    return cached;
            }

            string value = RawReadString(Section, Ident, Default);

            lock (_cacheLock)
            {
                if (fc != null) fc.Values[ck] = value;
            }
            return value;
        }

        // 真正的 P/Invoke 读（被 ReadString 缓存封装调用），保持与原 ReadString 完全一致的行为
        private string RawReadString(string Section, string Ident, string Default)
        {
            try
            {
                Byte[] Buffer = new Byte[65535];
                int bufLen = GetPrivateProfileString(Section, Ident, Default, Buffer, Buffer.GetUpperBound(0), FileName);
                //必须设定0（系统默认的代码页）的编码方式，否则无法支持中文
                //bufLen 是 ANSI 字节数；GetString 必须只解码前 bufLen 字节，
                //否则中文(GBK 2字节/字)会按"字节数=字符数"去截，尾部引入 '\0' 脏字符
                string s = Encoding.GetEncoding(0).GetString(Buffer, 0, bufLen);
                return s.Trim();
            }
            catch
            {
                return Default;
            }
        }

        //读整数
        public int ReadInteger(string Section, string Ident, int Default)
        {
            string intStr = ReadString(Section, Ident, Convert.ToString(Default));
            int result;
            if (int.TryParse(intStr, out result))
                return result;
            return Default;
        }

        //写整数
        public void WriteInteger(string Section, string Ident, int Value)
        {
            WriteString(Section, Ident, Value.ToString());
        }

        //读double
        public double ReadDouble(string Section, string Ident, double Default)
        {
            string str = ReadString(Section, Ident, Convert.ToString(Default));
            double result;
            if (double.TryParse(str, out result))
                return result;
            return Default;
        }

        //写双精度
        public void WriteDouble(string Section, string Ident, double Value)
        {
            WriteString(Section, Ident, Value.ToString());
        }

        //读布尔
        public bool ReadBool(string Section, string Ident, bool Default)
        {
            string str = ReadString(Section, Ident, Convert.ToString(Default));
            bool result;
            if (bool.TryParse(str, out result))
                return result;
            return Default;
        }

        //写Bool
        public void WriteBool(string Section, string Ident, bool Value)
        {
            WriteString(Section, Ident, Convert.ToString(Value));
        }

        //从Ini文件中，将指定的Section名称中的所有Ident添加到列表中
        public void ReadSection(string Section, StringCollection Idents)
        {
            if (string.IsNullOrEmpty(FileName))
                return;

            ValidateCacheAgainstFile();   // ★外部改动检测(mtime)

            var fc = GetCache(FileName);
            List<string> cached = null;
            lock (_cacheLock)
            {
                if (fc != null && fc.SectionIdents.TryGetValue(Section, out var list))
                    cached = list;
            }

            if (cached == null)
            {
                Byte[] Buffer = new Byte[16384];
                int bufLen = GetPrivateProfileString(Section, null, null, Buffer, Buffer.GetUpperBound(0), FileName);
                var col = new StringCollection();
                GetStringsFromBuffer(Buffer, bufLen, col);
                var list = new List<string>();
                foreach (string s in col) list.Add(s);
                cached = list;
                lock (_cacheLock)
                {
                    if (fc != null) fc.SectionIdents[Section] = cached;
                }
            }

            Idents.Clear();
            foreach (string s in cached) Idents.Add(s);
        }

        private void GetStringsFromBuffer(Byte[] Buffer, int bufLen, StringCollection Strings)
        {
            Strings.Clear();
            if (bufLen != 0)
            {
                int start = 0;
                for (int i = 0; i < bufLen; i++)
                {
                    if ((Buffer[i] == 0) && ((i - start) > 0))
                    {
                        String s = Encoding.GetEncoding(0).GetString(Buffer, start, i - start);
                        Strings.Add(s);
                        start = i + 1;
                    }
                }
            }
        }

        //从Ini文件中，读取所有的Sections的名称
        public void ReadSections(StringCollection SectionList)
        {
            if (string.IsNullOrEmpty(FileName))
                return;

            ValidateCacheAgainstFile();   // ★外部改动检测(mtime)

            var fc = GetCache(FileName);
            List<string> cached = null;
            lock (_cacheLock)
            {
                if (fc != null && fc.AllSections != null)
                    cached = fc.AllSections;
            }

            if (cached == null)
            {
                byte[] Buffer = new byte[65535];
                int bufLen = GetPrivateProfileString(null, null, null, Buffer, Buffer.GetUpperBound(0), FileName);
                var col = new StringCollection();
                GetStringsFromBuffer(Buffer, bufLen, col);
                var list = new List<string>();
                foreach (string s in col) list.Add(s);
                cached = list;
                lock (_cacheLock)
                {
                    if (fc != null) fc.AllSections = cached;
                }
            }

            SectionList.Clear();
            foreach (string s in cached) SectionList.Add(s);
        }

        //读取指定的Section的所有Value到列表中
        public void ReadSectionValues(string Section, NameValueCollection Values)
        {
            StringCollection KeyList = new StringCollection();
            ReadSection(Section, KeyList);
            Values.Clear();
            foreach (string key in KeyList)
            {
                Values.Add(key, ReadString(Section, key, ""));
            }
        }

        //清除某个Section
        public void EraseSection(string Section)
        {
            if (!WritePrivateProfileString(Section, null, null, FileName))
            {
                throw new ApplicationException("无法清除Ini文件中的Section: " + Section);
            }
            InvalidateSection(GetCache(FileName), Section);
            TouchCacheMtime(GetCache(FileName));   // ★自家写：刷新 mtime
        }

        //删除某个Section下的键
        public void DeleteKey(string Section, string Ident)
        {
            WritePrivateProfileString(Section, Ident, null, FileName);
            var fc = GetCache(FileName);
            if (fc != null)
            {
                lock (_cacheLock)
                {
                    fc.Values.Remove(CacheKey(Section, Ident));
                    fc.SectionIdents.Remove(Section);
                }
                TouchCacheMtime(fc);   // ★自家写：刷新 mtime
            }
        }

        //执行完对Ini文件的修改之后，应该调用本方法更新缓冲区。
        public void UpdateFile()
        {
            WritePrivateProfileString(null, null, null, FileName);
        }

        //检查某个Section下的某个键值是否存在
        public bool ValueExists(string Section, string Ident)
        {
            StringCollection Idents = new StringCollection();
            ReadSection(Section, Idents);
            return Idents.IndexOf(Ident) > -1;
        }

        /// <summary>
        /// P7：作废本实例对应文件的读缓存（如文件被外部修改后调用）。
        /// </summary>
        public void ClearCache()
        {
            var fc = GetCache(FileName);
            if (fc != null)
            {
                lock (_cacheLock)
                {
                    fc.Values.Clear();
                    fc.SectionIdents.Clear();
                    fc.AllSections = null;
                }
            }
        }

        /// <summary>
        /// P7：作废指定 INI 文件的读缓存。
        /// </summary>
        public static void ClearCache(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            string key = Path.GetFullPath(fileName);
            lock (_cacheLock)
            {
                _fileCaches.Remove(key);
            }
        }

        /// <summary>
        /// ★ N4：显式刷新并释放。
        /// 原实现在终结器里调用 <see cref="UpdateFile"/>，等于在 GC 线程做磁盘 IO（P/Invoke 写文件），
        /// 时机不确定、进程退出时也不保证执行；这里移除终结器，改为由持有方显式调用。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { UpdateFile(); } catch { }
            GC.SuppressFinalize(this);
        }
    }
}
