using System;
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
        }

        //读取INI文件指定
        public string ReadString(string Section, string Ident, string Default)
        {
            if (string.IsNullOrEmpty(FileName))
                return Default;

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

            Byte[] Buffer = new Byte[16384];
            int bufLen = GetPrivateProfileString(Section, null, null, Buffer, Buffer.GetUpperBound(0), FileName);
            GetStringsFromBuffer(Buffer, bufLen, Idents);
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

            byte[] Buffer = new byte[65535];
            int bufLen = GetPrivateProfileString(null, null, null, Buffer, Buffer.GetUpperBound(0), FileName);
            GetStringsFromBuffer(Buffer, bufLen, SectionList);
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
        }

        //删除某个Section下的键
        public void DeleteKey(string Section, string Ident)
        {
            WritePrivateProfileString(Section, Ident, null, FileName);
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
